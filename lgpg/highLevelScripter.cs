using System;
using System.Collections.Generic;
using System.Linq;

namespace lgpg
{
    public class highLevelScripter
    {
        public List<string> data = new List<string>();
        public static List<string> gateTypes = new List<string>{ "and", "or", "not", "xor"};
        public List<string> customCircuits = new List<string>();
        public Dictionary<string, List<string>> circuitUsedNames = new Dictionary<string, List<string>>();
        private static int c;
        public char delimiter = ';';
        public List<string> compilationErrors = new List<string>();
        private string currentCircuit = "sandbox";
        private bool parsingCircuit = false;
        private int numConn = 0;

        public List<string> compileLine(string str)
        {
            //Example script:
            //init 3,1; //init the board 3 inputs, 1 output
            //and myAnd,3; //create an and gate called myAnd
            //not myNot;
            //in[]->myAnd[]->myNot[]->out[] // or in->myAnd->myNot->out
            //end
            //Circuit usage
            str = str.TrimStart(' ').TrimEnd(' ');
            string[] parts = str.TrimStart(' ').TrimEnd(' ').Split(' ');
            if (parts[0].ToLower() == "init")
            {
                //init <numIn>,<numOut>;
                //Init the board
                if(1 < parts.Length)
                {
                    string[] arguments = parts[1].Split(',');
                    if (1 < arguments.Length)
                    {
                        foreach (string param in arguments) if (!Int32.TryParse(param, out c)) throw new Exception("argument type error");
                        foreach (string param in arguments) if (!(0 < Int32.Parse(param))) throw new Exception("argument range error");
                        return new List<string> { "init in=" + arguments[0] + ",out=" + arguments[1] };
                    } 
                    else
                    {
                        //Error
                        throw new Exception("argument length error");
                    }
                } 
                else 
                {
                    //Error
                    throw new Exception("argument length error");
                }
            } 
            else if (gateTypes.Contains(parts[0])) //Gate names and circuit names are case sensitive
            {
                //Gate create declaration
                //example: <gatename> <name>,<numinputs>;
                //example: <gatename> <name>;
                if (1 < parts.Length)
                {
                    string[] arguments = parts[1].Split(',');
                    if (1 < arguments.Length)
                    {
                        if (!Int32.TryParse(arguments[1], out c)) throw new Exception("argument type error");

                        circuitUsedNames[currentCircuit].Add(arguments[0]);
                        return new List<string> { "creategate name=" + arguments[0] + ",inputNum=" + arguments[1] + ",type=" + parts[0] };

                    }
                    else if(0 < arguments.Length)
                    {
                        circuitUsedNames[currentCircuit].Add(arguments[0]);
                        return new List<string> { "creategate name=" + arguments[0] + ",type=" + parts[0] };
                    }
                    else
                    {
                        //Error
                        throw new Exception("argument length error");
                    }
                }
                else
                {
                    //Error
                    throw new Exception("argument length error");
                }

            } 
            else if (customCircuits.Contains(parts[0]))
            {
                //circuit create decleration:
                //example: <circuit name> <instance name>;
                if (1 < parts.Length)
                {
                    return new List<string> { "creategate name=" + parts[1] + ",type=" + parts[0] };
                }
                else
                {
                    //Error
                    throw new Exception("argument length error");
                }
            }
            //Here comes the hard syntax part D:
            else if (1 < parts.Length)
            {
                //circuit definition decleration:
                //example: <circuit name> {<circuit connections and stuff>};
                if (parts[1].StartsWith("{", StringComparison.InvariantCulture) && parts[1].EndsWith("}", StringComparison.InvariantCulture)) // Check for the circuit body
                {
                    //Circuit body exists
                    if (parsingCircuit) throw new Exception("Nested bodies are not allowed");
                    parsingCircuit = true;
                    List<string> ret = new List<string>();
                    string commands = parts[1].TrimStart('{').TrimEnd('}'); //Remove body notation
                    try
                    {
                        List<string> individual = tokenize(commands);

                        string inNum = "0";
                        string outNum = "0";

                        if ((0 < individual.Count))
                        {
                            parsingCircuit = false;
                            throw new Exception("empty body");
                        }
                        
                        if ((0 < individual[0].Split(' ').Length))
                        {
                            parsingCircuit = false;
                            throw new Exception("argument length error");
                        }
                            
                        try
                        {
                            compileLine(individual[0]);
                        }
                        catch (Exception ex)
                        {
                            parsingCircuit = false;
                            throw ex;
                        }

                        if (individual[0].Split(' ')[0].ToLower() == "init")
                        {
                            string initCommandArgs = individual[0].Split(' ')[1];
                            inNum = initCommandArgs.Split(',')[0];
                            outNum = initCommandArgs.Split(',')[1];
                            individual.RemoveAt(0);
                        } 
                        else 
                        {
                            parsingCircuit = false;
                            throw new Exception("first body command must be init");
                        }

                        currentCircuit += "." + parts[0];

                        int upTil;

                        if (circuitUsedNames.ContainsKey(currentCircuit))
                        {
                            parsingCircuit = false;
                            upTil = currentCircuit.Length - (parts[0].Length + 1);
                            currentCircuit = currentCircuit.Substring(0, upTil);
                            throw new Exception("circuit already exists");
                        }

                        foreach (string statement in individual)
                        {
                            try
                            {
                                ret.AddRange(compileLine(statement));
                            }
                            catch (Exception ex)
                            {
                                parsingCircuit = false;
                                throw ex;
                            }
                        }
                        circuitUsedNames.Add(currentCircuit, new List<string> { "in", "out" });
                        ret.Insert(0, "createcircuit name=" + parts[0] + ",in=" + inNum + ",out=" + outNum);
                        upTil = currentCircuit.Length - (parts[0].Length + 1);
                        currentCircuit = currentCircuit.Substring(0, upTil);
                        parsingCircuit = false;

                        return ret;
                    }
                    catch (Exception ex)
                    {
                        parsingCircuit = false;
                        throw ex;
                    }
                }
            } 
            if(str.Contains("=>")) //else removed because "new not=>new not" wont work because upper is true but sub is false
            {
                //It's a connection diagram
                //<gate>=><gate>=><gate>
                //<gate>[]=><gate>[]=><gate>[]
                //<gate>[<portnum>]=><gate>[<portnum>]=><gate>[<portnum>]
                //<gate>=><gate>[]=><gate>[<portnum>]
                List<string> pts = utilities.split("=>", str).ToList();
                for (int i = 0; i < pts.Count; i++)
                {
                    pts[i] = pts[i].TrimStart(' ').TrimEnd(' ');
                }
                if (!(1 < pts.Count)) throw new Exception("connection argument length error");
                List<string> ret = new List<string>();

                //For the new gate thing
                if ("new ".Length <= str.Length) if (str.Substring(0, "new ".Length) == "new ")
                {
                    //create new gate in connection
                    //new and,3
                    //new not
                    int occurrance = 0;

                    string gateName = "";
                    foreach (char ch in pts[0])
                    {
                        if (!(ch.Equals("[") || ch.Equals("]")))
                        {
                            gateName += ch;
                        }
                    }

                    string[] gtpts = gateName.Split(' ');
                    if (!(1 < gtpts.Length)) throw new Exception("argument length error");
                    if (1 < gtpts.Length)
                    {
                        if (2 < gtpts.Length)
                        {
                            ret.AddRange(compileLine(gtpts[1] + " " + (numConn + "." + gtpts[1] + "." + occurrance) + "," + gtpts[2]));
                        }

                        if (1 < gtpts.Length)
                        {
                            ret.AddRange(compileLine(gtpts[1] + " " + (numConn + "." + gtpts[1] + "." + occurrance)));
                        }
                    }
                }
                //

                for (int i = 1; i < pts.Count; i++)
                {
                    bool all = false;
                    string srcGate = "";
                    int[] srcPorts = { };
                    string destGate = "";
                    int[] destPorts = { };

                    bool parsingBrackets = false;
                    string innerBrackets = "";
                    int countBrackets = 0;
                    string gateName = "";
                    foreach (char ch in pts[i])
                    {
                        if (ch.Equals("["))
                        {
                            parsingBrackets = true;
                            countBrackets++;
                        }
                        else if (parsingBrackets)
                        {
                            innerBrackets += ch;
                        }
                        else if (ch.Equals("]"))
                        {
                            parsingBrackets = false;
                            countBrackets++;
                        }
                        else
                        {
                            gateName += ch;
                        }
                    }

                    if ("new ".Length <= gateName.Length) if (gateName.Substring(0, "new ".Length) == "new ")
                    {
                        //create new gate in connection
                        //new and,3
                        //new not
                        int occurrance = 0;
                        for (int j = 0; j < pts.Count; j++)
                        {
                            if (i == j) break;
                            if (pts[j].Contains("new ")) occurrance++; //Possible bug
                        }

                        string[] gtpts = gateName.Split(' ');
                        if (!(1 < gtpts.Length)) throw new Exception("argument length error");
                        if (1 < gtpts.Length)
                        {
                            if (2 < gtpts.Length) 
                            {
                                ret.AddRange(compileLine(gtpts[1] + " " + (numConn + "." + gtpts[1] + "." + occurrance) + "," + gtpts[2]));
                            }

                            if (1 < gtpts.Length) 
                            {
                                ret.AddRange(compileLine(gtpts[1] + " " + (numConn + "." + gtpts[1] + "." + occurrance)));
                            }
                        }

                        gateName = (numConn + "." + gtpts[1] + "." + occurrance);
                    }

                    if (countBrackets % 2 != 0) throw new Exception("unblanced brackets");
                    if (innerBrackets == "")
                        all = true;
                    else
                    {
                        string[] possPorts = innerBrackets.Split(',');
                        foreach(string pp in possPorts) if (!Int32.TryParse(pp, out c)) throw new Exception("argument type error");
                        List<int> ports = new List<int>();
                        foreach (string pp in possPorts) ports.Add(Int32.Parse(innerBrackets));
                        destPorts = ports.ToArray();
                    }
                    if (!circuitUsedNames[currentCircuit].Any((x)=>x.Equals(gateName))) throw new Exception("gate '" + gateName + "' doesn't exist");
                    destGate = gateName;

                    parsingBrackets = false;
                    innerBrackets = "";
                    countBrackets = 0;
                    gateName = "";
                    foreach (char ch in pts[i-1])
                    {
                        if (ch.Equals("["))
                        {
                            parsingBrackets = true;
                            countBrackets++;
                        }
                        else if (parsingBrackets)
                        {
                            innerBrackets += ch;
                        }
                        else if (ch.Equals("]"))
                        {
                            parsingBrackets = false;
                            countBrackets++;
                        }
                        else
                        {
                            gateName += ch;
                        }
                    }
                    if (countBrackets % 2 != 0) throw new Exception("unblanced brackets");
                    if (innerBrackets == "")
                    {
                        if (!all)
                        {
                            throw new Exception("source desintination size mismatch");
                        }
                    }
                    else
                    {
                        string[] possPorts = innerBrackets.Split(',');
                        foreach (string pp in possPorts) if (!Int32.TryParse(pp, out c)) throw new Exception("argument type error");
                        List<int> ports = new List<int>();
                        foreach (string pp in possPorts) ports.Add(Int32.Parse(innerBrackets));
                        srcPorts = ports.ToArray();
                    }

                    if ("new ".Length <= gateName.Length) if (gateName.Substring(0, "new ".Length) == "new ")
                    {
                        //create new gate in connection
                        //new and,3
                        //new not
                        int occurrance = 0;
                        for (int j = 0; j < pts.Count; j++)
                        {
                            if ((i-1) == j) break;
                            if (pts[j].Contains("new ")) occurrance++; //Possible bug
                        }

                        string[] gtpts = gateName.Split(' ');
                        if (!(1<gtpts.Length)) throw new Exception("argument length error");

                        gateName = (numConn + "." + gtpts[1] + "." + occurrance);
                    }

                    if (!circuitUsedNames[currentCircuit].Any((x) => x.Equals(gateName))) throw new Exception("gate '" + gateName + "' doesn't exist");
                    srcGate = gateName;

                    if (srcPorts.Length != destPorts.Length) throw new Exception("source desintination size mismatch");

                    if(!all) 
                    {
                        for (int j = 0; j < destPorts.Length; j++)
                        {
                            ret.Add("connect from=" + srcGate + ",fp=" + srcPorts[j] + ",to=" + destGate + ",tp=" + destPorts[j]);
                        }
                    } 
                    else 
                    {
                        ret.Add("connect from=" + srcGate + ",to=" + destGate);
                    }
                }
                numConn++;
                return ret;
            }

            throw new Exception("non-existant command");
        }

        public lgSimInter toObject()
        {
            lgSimInter simInter = new lgSimInter();
            string dt = "";
            for (int i = 0; i < data.Count; i++)
            {
               dt += data[i] + "\n";
            }
            //dt = dt.Remove(dt.Length - 3); //remove last \n
            dt = dt.TrimEnd('\n');
            simInter.load(dt);
            simInter.run();
            return simInter;
        }

        public List<string> compile(string str)
        {
            List<string> compiledLines = new List<string>();
            str = str.Replace(Environment.NewLine, string.Empty);
            List<string> parts;
            try
            {
                parts = tokenize(str);
            }
            catch (Exception ex)
            {
                compilationErrors.Add(ex.Message);
                return null;
            }
            List<string> rawData = parts;
            rawData = rawData.Where(x => !string.IsNullOrEmpty(x)).ToList();

            for (int i = 0; i < parts.Count; i++)
            {
                try
                {
                    compiledLines.AddRange(compileLine(rawData[i]));
                }
                catch (Exception ex)
                {
                    compilationErrors.Add("Line " + i + ": " + ex.Message);
                }
            }

            return compiledLines;
        }

        public string startCompile(string str)
        {
            compilationErrors.Clear();
            circuitUsedNames.Clear();
            data.Clear();
            numConn = 0;
            if (!circuitUsedNames.ContainsKey("sandbox")) circuitUsedNames.Add("sandbox", new List<string>{"in", "out"});
            List<string> compiled = compile(str);
            if (0 < compilationErrors.Count)
            {
                string ret = "Errors:";
                foreach (string error in compilationErrors)
                {
                    ret += error + "\n";
                }
                return ret;
            }

            data.AddRange(compiled);
            return "Ok " + compiled.Count + " lines generated";
        }

        public List<string> tokenize(string input)
        {
            List<string> parts = new List<string>();
            int numBraces = 0; //Number of '{' or '}'
            bool split = true;
            string current = "";
            foreach (char ch in input)
            {
                if (split)
                {
                    if (ch.Equals(delimiter))
                    {
                        parts.Add(current);
                        current = "";
                    }
                    else if (ch.Equals('{'))
                    {
                        numBraces++;
                        split = false;
                    }
                    else if (ch.Equals('}'))
                    {
                        numBraces++;
                        split = true;
                    }
                    else
                    {
                        current += ch;
                    }
                }
            }
            if (numBraces % 2 != 0) throw new Exception("unblanced braces");
            return parts;
        }
    }
}
