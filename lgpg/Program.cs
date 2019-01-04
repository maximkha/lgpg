using System;
using System.Threading;
using System.Linq;
using System.IO;

namespace lgpg
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0) Console.WriteLine(args[0]);

            lgSimInter logicSim = new lgSimInter();
            bool consoleMode = false;

            highLevelScripter scripter = null;
            bool hl = false;
            string dt = "";

            //Some settings
            //consoleMode = true;
            //hl = true;
            //scripter = new highLevelScripter();
            //

            while(true)
            {
                string input = Console.ReadLine();
                if(input.Length>0)
                {
					if (input[0] == '$')
					{
						consoleMode = !consoleMode;
						input = string.Join(string.Empty, input.Skip(1));
					}
                }

                if (consoleMode)
                {
                    //Console.WriteLine("INFO: You have intered console mode!");
                    if (input == "exit") break;
                    if (input == "load")
                    {
                        Console.Write("Filename> ");
                        string data = System.IO.File.ReadAllText(Console.ReadLine());
                        string co = scripter.startCompile(data);
                        Console.WriteLine(co);
                        if (!co.Contains("Ok")) return;
                        logicSim = scripter.toObject();
                        //logicSim.load(data);
                        //logicSim.run();
                        consoleMode = false;
                        //Console.WriteLine("INFO: You have exited console mode!");
                    }

                    if (input == "ping") Console.WriteLine("pong");

                    if (input == "reset")
                    {
                        logicSim.sandbox = new components.circuitMain.circuit();
                    }

                    if (input == "compile")
                    {
                        string co = scripter.startCompile(dt);
                        Console.WriteLine(co);
                        if(!co.Contains("Ok")) return;
                        logicSim = scripter.toObject();
                        consoleMode = false;
                    }

                    if (hl)
                    {
                        if (input == "end")
                        {
                            hl = false;
                        } 
                        else 
                        {
                            dt += input + "\n";
                        }
                    }

                    if (input == "hl")
                    {
                        hl = true;
                        scripter = new highLevelScripter();
                    }
                }
                else 
                { 
                    Console.WriteLine(logicSim.run(input)); 
                }
            }

            Console.WriteLine("Goodbye!");
            Thread.Sleep(250);
        }
    }
}
