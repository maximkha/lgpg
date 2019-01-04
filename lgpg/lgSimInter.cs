using System;
using System.Collections.Generic;

namespace lgpg
{
    public class lgSimInter
    {
        private string tData = "";
        public components.circuitMain.circuit sandbox = new components.circuitMain.circuit();
        private bool circuitCreationMode = false;
        private string newCircuitName = "";
        private Dictionary<string, components.circuitMain.circuit> circuits = new Dictionary<string, components.circuitMain.circuit>();
        private components.circuitMain.circuit sandboxOld = null;

        public lgSimInter()
        {

        }

        public string run(string commandArg)
        {
            string[] cParts = commandArg.Split(' ');
            //if (cParts.Length == 0) return "Error: Blank input!";
            //if (cParts.Length <= 1) return "";//ignore empty statements
            if (commandArg == "") return "Ok";
            //Console.WriteLine(cParts.Length);

            if (cParts.Length > 2)
            {
                //added extra space for beauty probably.
                string[] args = (string[])cParts.Clone();
                string sargs = "";
                bool first = true;
                foreach (string arg in args)
                {
                    sargs += arg.Replace(" ", ""); //unbeautify it (:<
                    if (first)
                    {
                        sargs += " ";
                        first = false;
                    }
                }
                cParts = sargs.Split(' ');
            }

            Dictionary<string, string> propertyValue = new Dictionary<string, string>();

            if (cParts.Length > 1)
            {
                string[] propDefs = cParts[1].Split(',');
                foreach (string propDef in propDefs)
                {
                    string[] defParts = propDef.Split('=');

                    if (2 != defParts.Length)
                    {
                        if (0 < defParts.Length) { return "Error: No value at keyword: '" + defParts[0] + "'!"; }
                        else
                        {
                            return "Error: No name or value!";
                        }
                    }

                    propertyValue.Add(defParts[0], defParts[1]);
                }
            }

            string command = cParts[0];
            if (command == "creategate")
            {
                int ni = -1;
                string gatename = "";
                string gateType = "";

                if (!propertyValue.ContainsKey("name")) return "Error: Gate name was not specified!";
                if (!propertyValue.ContainsKey("type")) return "Error: Gate type was not specified!";

                if (propertyValue.ContainsKey("inputNum")) ni = Convert.ToInt32(propertyValue["inputNum"]);
                gatename = propertyValue["name"];
                gateType = propertyValue["type"];

                return createGate(gateType, gatename, ni);
            }
            else if (command == "connect")
            {
                if (!propertyValue.ContainsKey("from")) return "Error: No from gate was specified!";
                if (!propertyValue.ContainsKey("to")) return "Error: No to gate was specified!";
                if (!sandbox.gates.ContainsKey(propertyValue["from"])) return "Error: Gate with name '" + propertyValue["from"] + "' does not exist!";
                if (!sandbox.gates.ContainsKey(propertyValue["to"])) return "Error: Gate with name '" + propertyValue["to"] + "' does not exist!";

                if (propertyValue.ContainsKey("fp") || propertyValue.ContainsKey("tp"))
                {
                    string emp = propertyValue.ContainsKey("fp") ? "to" : "from";
                    if (!(propertyValue.ContainsKey("fp") && propertyValue.ContainsKey("tp"))) return "Error: " + emp + " port needs to be specified!";
                    int c;
                    if (!Int32.TryParse(propertyValue["fp"], out c)) return "Error: From port must be integer!";
                    if (!Int32.TryParse(propertyValue["tp"], out c)) return "Error: To port must be integer!";
                    if (sandbox.inputs.Length < Convert.ToInt32(propertyValue["fp"])) return "Error: Gate '" + propertyValue["from"] + "' has only " + sandbox.gates[propertyValue["from"]].outputs.Length + " outputs but tried to access output " + Convert.ToInt32(propertyValue["fp"]) + "!";
                    if (sandbox.inputs.Length < Convert.ToInt32(propertyValue["tp"])) return "Error: Gate '" + propertyValue["to"] + "' has only " + sandbox.gates[propertyValue["to"]].inputs.Length + " inputs but tried to access input " + Convert.ToInt32(propertyValue["tp"]) + "!";
                    return connect(sandbox.gates[propertyValue["from"]], sandbox.gates[propertyValue["to"]], Convert.ToInt32(propertyValue["fp"]), Convert.ToInt32(propertyValue["tp"]));
                }

                if (sandbox.gates[propertyValue["from"]].outputs.Length != sandbox.gates[propertyValue["to"]].inputs.Length) return "Error: Gate '"+propertyValue["from"] +"' has "+ sandbox.gates[propertyValue["from"]].outputs.Length +" outputs while gate '"+ propertyValue["to"] +"' has " + sandbox.gates[propertyValue["to"]].inputs.Length + "inputs!";
                return connect(sandbox.gates[propertyValue["from"]], sandbox.gates[propertyValue["to"]],-1,-1);
            }
            else if (command == "in")
            {
                //Format: in <inputnum>=<inputval>,<inputnum>=<inputval>
                int c = 0;
                foreach (string param in propertyValue.Keys) if (!Int32.TryParse(param, out c)) return "Error: All parameters must be intergers!";

                foreach (string param in propertyValue.Values) if (!Int32.TryParse(param, out c)) return "Error: All values must be integers!";

                int bignum = 0;
                foreach (string param in propertyValue.Keys) if (Convert.ToInt32(param) > bignum) bignum = Convert.ToInt32(param);

                if (sandbox.inputs.Length < bignum) return "Error: There are only " + sandbox.inputs.Length + " inputs but tried to access input " + bignum + "!";

                int maxV = 0;
                int minV = 100;
                foreach (string v in propertyValue.Values) if (Convert.ToInt32(v) > maxV) maxV = Convert.ToInt32(v);
                foreach (string v in propertyValue.Values) if (Convert.ToInt32(v) < minV) minV = Convert.ToInt32(v);
                if (minV < 0 || maxV > 1) return "Error: Values must be between 0 and 1!";

                foreach (KeyValuePair<string, string> kvp in propertyValue)
                {
                    sandbox.inputs[Convert.ToInt32(kvp.Key)] = Convert.ToInt32(kvp.Value);
                }
                return "Ok";
            }
            else if (command == "init")
            {
                if (!propertyValue.ContainsKey("in")) return "Error: Number of inputs not specified!";
                if (!propertyValue.ContainsKey("out")) return "Error: Number of outputs not specified!";
                int c = 0;
                if (!Int32.TryParse(propertyValue["in"], out c)) return "Error: Number of inputs must be a integer!";
                if (!Int32.TryParse(propertyValue["out"], out c)) return "Error: Number of outputs must be a integer!";
                sandbox.init(Convert.ToInt32(propertyValue["in"]), Convert.ToInt32(propertyValue["out"]), true);
                return "Ok";
            }
            else if (command == "tick")
            {
                sandbox.tick();
                return "\nOk";
            } 
            else if (command == "createcircuit") //Basicly creates a 'type'
            {
                if (circuitCreationMode) return "Error: Already creating a circuit!";
                if (!propertyValue.ContainsKey("in")) return "Error: Number of inputs not specified!";
                if (!propertyValue.ContainsKey("out")) return "Error: Number of outputs not specified!";
                if (!propertyValue.ContainsKey("name")) return "Error: Circuit name was not specified!";
                int c = 0;
                if (!Int32.TryParse(propertyValue["in"], out c)) return "Error: Number of inputs must be a integer!";
                if (!Int32.TryParse(propertyValue["out"], out c)) return "Error: Number of outputs must be a integer!";
                createCircuit(Convert.ToInt32(propertyValue["in"]), Convert.ToInt32(propertyValue["out"]), propertyValue["name"]);
                return "Ok";
            }
            else if (command == "end")
            {
                if (circuitCreationMode)
                {
                    circuitCreationMode = false;
                    circuits.Add(newCircuitName, sandbox);
                    sandbox = sandboxOld;
                    sandboxOld = null;
                    ((components.output)sandbox.gates["out"]).callExists = false;
                    return "Ok";
                } else {
                    return "Error: Can't end circuit creation when it hasn't started";
                }
            }
            return "Error: unknown command '" + command + "'!";
        }

        public string createCircuit(int inNum, int outNum, string name)
        {
            sandboxOld = sandbox;
            sandbox = new components.circuitMain.circuit();
            circuitCreationMode = true;
            newCircuitName = name;
            run("init in=" + inNum + ",out=" + outNum); //initialize
            //sandbox.init(inNum, outNum, false);
            return "Ok";
        }

        public string connect(components.gate from, components.gate to, int fp, int tp)
        {
            components.circuitMain.connection c;
            if (fp > -1 && tp > -1) 
            { 
                c = new components.circuitMain.connection(from, to, fp, tp); 
            }
            else
            {
                c = new components.circuitMain.connection(from, to);
            }
            sandbox.connections.Add(c);

            return "Ok";
        }

        public string createGate(string gateType, string gateName, int inputnum)
        {
            components.gate theGate;

            if (sandbox.gates.ContainsKey(gateName)) return "Error: Gate with that name already exists!";

            if (gateType == "not")
            {
                theGate = new components.notGate();
                if (inputnum != -1 && inputnum != 1)
                {
                    return "Warning: Not gate can only have 1 input, defaulting to 1 input";
                }
                else
                {
                    sandbox.gates.Add(gateName, theGate);
                    return "Ok";
                }
            }

            if (inputnum == -1) inputnum = 2;

            if (gateType == "and")
            {
                theGate = new components.andGate(inputnum);
                sandbox.gates.Add(gateName, theGate);
                return "Ok";
            }

            if (gateType == "or")
            {
                theGate = new components.orGate(inputnum);
                sandbox.gates.Add(gateName, theGate);
                return "Ok";
            }

            if (gateType == "xor")
            {
                theGate = new components.xorGate();
                if (inputnum != -1 && inputnum != 2)
                {
                    return "Warning: Not gate can only have 2 inputs, defaulting to 2 inputs";
                }
                else
                {
                    sandbox.gates.Add(gateName, theGate);
                    return "Ok";
                }
            }

            if (circuits.ContainsKey(gateType))
            {
                theGate = circuits[gateType].clone(); //So elegant :D
                sandbox.gates.Add(gateName, theGate);
                return "Ok";
            }

            return "Error: unknown gate or component";

        }

        public string run()
        {
            string ro = "";
            if (tData == "") { return "No data"; }
            string[] commands = tData.Split('\n');
            foreach(string command in commands){
                //Console.WriteLine(run(command));
                string commOut = run(command);
                if (commOut != "Ok")
                {
                    ro += command + ": " + commOut + "\n";
                }
            }
            if (ro == "") ro += "Ok";
            if (ro != "Ok") ro = ro.Remove(ro.Length - 3);
            return ro;
        }

        public void load(string data)
        {
            tData = data;
        }
    }
}
