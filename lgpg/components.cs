using System;
using System.Collections.Generic;
using System.Linq;

namespace lgpg
{
    public static class components
    {

        //TODO: implement toggle gate and sr latch. PS: implement a 
        //trigger circuit
        //hint: use previous state of inputs

        public interface gate
        {
            void tick();
            gate clone();
            bool isValid();
            int[] inputs { get; set; }
            int[] outputs { get; set; }
        }

        public class andGate : gate
        {
            public int[] inputs { get; set; }
            public int[] outputs { get; set; }

            public andGate(int numInput)
            {
                outputs = new int[1];
                outputs[0] = 0;
                inputs = new int[numInput];
            }

            public void tick()
            {
                outputs[0] = 1;
                foreach (int input in inputs)
                {
                    if (input == 0) { outputs[0] = 0; return; }
                }
            }

            public gate clone()
            {
                gate newGate = new andGate(inputs.Length);
                newGate.inputs = inputs;
                newGate.outputs = outputs;
                return newGate;
            }

            public bool isValid()
            {
                int[] tmpOut = outputs;
                tick();
                bool ret = tmpOut.Equals(outputs);
                outputs = tmpOut;
                return ret;
            }
        }

        public class orGate : gate
        {
            public int[] inputs { get; set; }
            public int[] outputs { get; set; }

            public orGate(int numInput)
            {
                outputs = new int[1];
                outputs[0] = 0;
                inputs = new int[numInput];
            }

            public void tick()
            {
                outputs[0] = 0;
                foreach (int input in inputs)
                {
                    if (input == 1) { outputs[0] = 1; return; }
                }
            }

            public gate clone()
            {
                gate newGate = new orGate(inputs.Length);
                newGate.inputs = inputs;
                newGate.outputs = outputs;
                return newGate;
            }

            public bool isValid()
            {
                int[] tmpOut = outputs;
                tick();
                bool ret = tmpOut.Equals(outputs);
                outputs = tmpOut;
                return ret;
            }
        }

        public class notGate : gate
        {
            public int[] inputs { get; set; }
            public int[] outputs { get; set; }

            public notGate()
            {
                outputs = new int[1];
                outputs[0] = 0;
                inputs = new int[1];
            }

            public void tick()
            {
                outputs[0] = 0;
                foreach (int input in inputs)
                {
                    if (input == 1) { outputs[0] = 0; return; }
                    if (input == 0) { outputs[0] = 1; return; }
                }
            }

            public gate clone()
            {
                gate newGate = new notGate();
                newGate.inputs = inputs;
                newGate.outputs = outputs;
                return newGate;
            }

            public bool isValid()
            {
                int[] tmpOut = outputs;
                tick();
                bool ret = tmpOut.Equals(outputs);
                outputs = tmpOut;
                return ret;
            }
        }

        public class xorGate : gate
        {
            public int[] inputs { get; set; }
            public int[] outputs { get; set; }

            public xorGate()
            {
                outputs = new int[1];
                outputs[0] = 0;
                inputs = new int[2];
            }

            public void tick()
            {
                outputs[0] = 0;

                int aN = 0;
                if (inputs[0] == 0) aN = 1;
                if (inputs[0] == 1) aN = 0;
                int bN = 0;
                if (inputs[1] == 0) bN = 1;
                if (inputs[1] == 1) bN = 0;

                int and1 = 0;
                if (aN == 1 && inputs[1] == 1) and1 = 1;
                int and2 = 0;
                if (bN == 1 && inputs[0] == 1) and2 = 1;

                int or = and1 + and2;
                if (or > 1) or = 1;

                outputs[0] = or;
            }

            public gate clone()
            {
                gate newGate = new xorGate();
                newGate.inputs = inputs;
                newGate.outputs = outputs;
                return newGate;
            }

            public bool isValid()
            {
                int[] tmpOut = outputs;
                tick();
                bool ret = tmpOut.Equals(outputs);
                outputs = tmpOut;
                return ret;
            }
        }

        public class input : gate
        {
            public int[] inputs { get; set; } //Not used, just to comply with interface
            public int[] outputs { get; set; }

            public bool callExists = false;
            Func<int[], int> callback;

            public input(int numOut)
            {
                if (numOut <= 0) numOut = 1;
                outputs = new int[numOut];
                outputs[0] = 0;
            }

            public input(Func<int[], int> cb, int numOut)
            {
                if (numOut <= 0) numOut = 1;
                outputs = new int[numOut];
                callExists = true;
                callback = cb;
            }

            public void tick()
            {
				//nothing here
				//nothing to prevent multiple ticking bug.
			}

			public void tick(bool t)
			{
				if (callExists) callback(inputs); //added for debug
			}

            public gate clone()
            {
                input newGate = new input(outputs.Length);
                newGate.inputs = inputs;
                newGate.outputs = outputs;
                newGate.callback = callback;
                newGate.callExists = callExists;
                return newGate;
            }

            //Comply with gate interface
            public bool isValid()
            {
                return true;
            }
        }

        public class output : gate
        {
            public int[] inputs { get; set; }
            public int[] outputs { get; set; } //Not used, just to comply with interface

            public bool callExists = false;
            Func<int[], int> callback;

            public output(int numIn)
            {
                if (numIn <= 0) numIn = 1;
                inputs = new int[numIn];
            }

            public output(Func<int[], int> cb, int numIn)
            {
                if (numIn <= 0) numIn = 1;
                inputs = new int[numIn];
                callExists = true;
                callback = cb;
            }

            public void tick()
            {
                //nothing to prevent multiple ticking bug.
            }

            public void tick(bool t)
            {
                if (callExists) callback(inputs); //added for debug
			}

            public gate clone()
            {
                output newGate = new output(inputs.Length);
                newGate.inputs = inputs;
                newGate.outputs = outputs;
                newGate.callback = callback;
                newGate.callExists = callExists;
                return newGate;
            }

            //Comply with gate interface
            public bool isValid()
            {
                return true;
            }
        }


        public static class circuitMain
        {
            public class connection
            {
                public gate fromComp;
                public gate toComp;
                public int fromPort;
                public int toPort;
                public bool all = false;

                public connection(gate fg, gate tg, int fp, int tp)
                {
                    fromComp = fg;
                    toComp = tg;
                    fromPort = fp;
                    toPort = tp;
                }

                public connection(gate fg, gate tg)
                {
                    fromComp = fg;
                    toComp = tg;
                    all = true;
                }

                public connection clone(gate newFrom, gate newTo)
                {
                    if (all) return new connection(newFrom, newTo);
                    return new connection(newFrom, newTo, fromPort, toPort);
                }
            }

            public class circuit : gate
            {
                public Dictionary<string, components.gate> gates = new Dictionary<string, components.gate>();
                public List<connection> connections = new List<connection>();

                public int[] inputs { get; set; }
                public int[] outputs { get; set; }
                public int tickMode = 0;

                private input inputGate;
                private output outputGate;

                public bool initialized = false;

                public void init(int i, int o, bool t)
                {
                    if (t)
                    {
                        inputs = new int[i];
                        outputs = new int[o];
                        inputGate = new input(i);
                        outputGate = new output(log, o);
                    }
                    else
                    {
                        inputs = new int[i];
                        outputs = new int[o];
                        inputGate = new input(i);
                        outputGate = new output(o);
                    }

                    gates.Add("in", inputGate);
                    gates.Add("out", outputGate);
                    initialized = true;
                }

                public int log(int[] outputs)
                {
                    Console.Write("output:");
                    bool first = true;
                    foreach (int output in outputs)
                    {
                        if (first)
                        {
                            Console.Write(output);
                            first = false;
                            continue;
                        }
                        Console.Write("," + output);
                    }
                    return 0;
                }

                //TODO: Check for Bugs

                public void tick()
                {
                    //if you only explore the path if an input should be updated that should allow for back connection
                    //Gotcha fam

                    inputGate.outputs = inputs;
                    inputGate.tick(true);

                    if (tickMode == 0)
                    {
                        //Dumb tick, deals with logic loops but isn't efficient
                        while (true)
                        {
                            foreach (gate g in gates.Values)
                            {
                                pushConnections();
                                g.tick();
                            }

                            if (this.isValid())
                            {
                                break;
                            }
                        }
                    } 
                    else if (tickMode == 1)
                    {
                        //Smart tick, doesn't do logic loops
                        Stack<gate> queuedComponents = new Stack<gate>();
                        queuedComponents.Push(inputGate);

                        while (queuedComponents.Count > 0)
                        {
                            gate current = queuedComponents.Pop();
                            List<connection> gateConnections = connectionsFromGate(current);
                            current.tick();

                            foreach (connection c in gateConnections)
                            {
                                if (c.all) c.toComp.inputs = c.fromComp.outputs;
                                if (!c.all) c.toComp.inputs[c.toPort] = c.fromComp.outputs[c.fromPort];
                                queuedComponents.Push(c.toComp);
                            }
                        }
                    }
                    else if (tickMode == 2)
                    {
                        //Even if not done complete tick
                        foreach (gate g in gates.Values)
                        {
                            pushConnections();
                            g.tick();
                        }
                    }

                    outputGate.tick(true);
                }


				private List<connection> connectionsFromGate(gate from)
                {
                    List<connection> ret = new List<connection>();

                    foreach (connection c in connections)
                    {
                        if (c.fromComp == from)
                        {
                            ret.Add(c);
                        }
                    }

                    return ret;
                }

                public gate clone()
                {
                    circuit newCircuit = new circuit();
                    newCircuit.init(inputs.Length, outputs.Length, outputGate.callExists);

                    for (int i = 0; i < gates.Count; i++)
                    {
                        KeyValuePair<string, gate> nameGate = gates.ElementAt(i);
                        if (nameGate.Key == "in" || nameGate.Key == "out") continue;
                        newCircuit.gates.Add(nameGate.Key, nameGate.Value.clone());
                    }

                    foreach (connection c in connections)
                    {
                        string fromName = lookupGate(c.fromComp);
                        string toName = lookupGate(c.toComp);
                        newCircuit.connections.Add(c.clone(newCircuit.gates[fromName], newCircuit.gates[toName]));
                    }

                    return newCircuit;
                }

                public string lookupGate(gate g)
                {
                    for (int i = 0; i < gates.Count; i++)
                    {
                        if (gates.Values.ElementAt(i).Equals(g))
                            return gates.Keys.ElementAt(i);
                    }

                    return null;
                }

                public bool isValid()
                {
                    return gates.Values.All((g)=>g.isValid());
                }

                public List<connection> connectionsToGate(gate to)
                {
                    List<connection> ret = new List<connection>();

                    foreach (connection c in connections)
                    {
                        if (c.toComp == to)
                        {
                            ret.Add(c);
                        }
                    }

                    return ret;
                }

                public void pushConnections()
                {
                    foreach (connection c in connections)
                    {
                        if (c.all) c.toComp.inputs = c.fromComp.outputs;
                        if (!c.all) c.toComp.inputs[c.toPort] = c.fromComp.outputs[c.fromPort];
                    }
                }
            }
        }
    }
}
