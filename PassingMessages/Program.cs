using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Util.Internal;
using Newtonsoft.Json;

namespace PassingMessages
{
    class Program
    {

        public class ActorX : ReceiveActor
        {
            public ActorX()
            {
                Receive<int>(m =>
                {
                    var result = 1;
                    for (int i = 1; i < m; i++)
                    {
                        result += new Random().Next(1, m);
                    }
                    Console.WriteLine("X:{0}", result);
                    Sender.Tell(result, Self);

                });
            }
        }

        public class ActorY : ReceiveActor
        {
            public ActorY()
            {
                Receive<int>(m =>
                {
                    int result = 1;
                    for (int i = 1; i < m; i++)
                    {
                        result += new Random().Next(1, m);
                    }
                    Console.WriteLine("Y:{0}", result);
                    Sender.Tell(result, Self);

                });
            }
        }






        internal class MessageZ
        {
            public int X { get; private set; }
            public int Y { get; private set; }

            public MessageZ(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        public class ActorZ : ReceiveActor
        {
            public ActorZ()
            {
                Receive<MessageZ>(m =>
                {
                    Console.WriteLine("x:{0} - y:{1}", m.X, m.Y);
                    var total = Int32.MaxValue;
                    Console.WriteLine("begin total : {0}", total);
                    while (total > 0)
                    {
                        total -= 1;

                        if (total > 0 && Int32.MaxValue % total == 1000000)
                        { 
                            Console.WriteLine(total);
                        }
                    }
                    Console.WriteLine("end total : {0}", total);

                });
            }
        }



        public class DevideZero : ReceiveActor
        {
            public DevideZero()
            {
                Receive<int>(m => DevideThis(m));
            }

            private void DevideThis(int i)
            {
                var randomNumbr = new Random().Next(1000, 2000);
                try
                {
                    var result = randomNumbr/i;
                    Sender.Tell(result, Self);
                }
                catch (Exception e)
                {
                    Sender.Tell(new Failure { Exception = e }, Self);
                }
            }
        }


        static void Main(string[] args)
        {





            var actorSystem = ActorSystem.Create("xyz");
            var xProxy = actorSystem.ActorOf<ActorX>("x");
            var yProxy = actorSystem.ActorOf<ActorY>("y");
            var zProxy = actorSystem.ActorOf<ActorZ>("z");
            var devideZeroProxy = actorSystem.ActorOf<DevideZero>("devideZero");


            var devideZeroProxyTask = devideZeroProxy.Ask(100, TimeSpan.FromSeconds(1));
            devideZeroProxyTask.Wait();



            var devideZeroProxyTask2 = devideZeroProxy.Ask(0, TimeSpan.FromSeconds(100));
            
            devideZeroProxyTask2.Wait();


            Console.WriteLine("devideZeroProxyResult : {0}", devideZeroProxyTask2.Result);
            Console.WriteLine("devideZeroProxyResult : {0}", JsonConvert.SerializeObject(devideZeroProxyTask2.Result));


            Console.ReadLine();


            Console.WriteLine("xProxy.Tell(10000);");
            var taskX = xProxy.Ask(100000, TimeSpan.FromSeconds(10));
            taskX.Wait();
            var resultX = taskX.Result;
            Console.WriteLine("resultX : {0}", resultX);



            Console.WriteLine("yProxy.Tell(10000);");
            var taskY = yProxy.Ask(100000, TimeSpan.FromSeconds(10));
            taskY.Wait();
            var resultY = taskY.Result;
            Console.WriteLine("taskY : {0}", resultY);

            Console.ReadLine();


            Console.WriteLine("Task Run");
            var t = Task.Run(async () =>
            {
                var taskX1 = xProxy.Ask(100000, TimeSpan.FromSeconds(10));

                //var taskX2 = taskX1.ContinueWith(c =>
                //{
                //   xProxy.Ask(taskX1.Result, TimeSpan.FromSeconds(10));
                //});


                var taskY1 = yProxy.Ask(100000, TimeSpan.FromSeconds(10));


                await Task.WhenAll(taskX1, taskY1);

                return new MessageZ((int)taskX1.Result, (int)taskY1.Result);


            });
            
            //var tResult = t.Result;

            Console.WriteLine("muahahhahah");
            //Console.WriteLine("{0} : t", JsonConvert.SerializeObject(tResult));

            t.PipeTo(zProxy);

            Console.WriteLine("this will execute immediately");


            Console.WriteLine("{0} : t - should be printed prior completition of the taskZ", JsonConvert.SerializeObject(t.Result));
            Console.ReadLine();




            Console.WriteLine("pattern practice");
            var myTask = Task.Run(async () =>
            {
                var taskX1 = xProxy.Ask(100000, TimeSpan.FromSeconds(10));
                var taskX2 = yProxy.Ask(100000, TimeSpan.FromSeconds(10));
                var taskX3 = xProxy.Ask(100000, TimeSpan.FromSeconds(10));
                var taskX4 = yProxy.Ask(100000, TimeSpan.FromSeconds(10));
                var taskX5 = xProxy.Ask(100000, TimeSpan.FromSeconds(10));
                var taskX6 = yProxy.Ask(100000, TimeSpan.FromSeconds(10));
                var taskX7 = xProxy.Ask(100000, TimeSpan.FromSeconds(10));
                var taskX8 = yProxy.Ask(100000, TimeSpan.FromSeconds(10));
                var taskX9 = xProxy.Ask(100000, TimeSpan.FromSeconds(10));
                var taskX10 = yProxy.Ask(100000, TimeSpan.FromSeconds(10));

                await Task.WhenAll(taskX1, taskX2, taskX3, taskX4, taskX5, taskX6, taskX7, taskX8, taskX9, taskX10);
                return new MessageZ((int)taskX10.Result, (int)taskX1.Result);


            });

            myTask.PipeTo(zProxy);

            Console.ReadLine();







            /////////////Forward message
            

        }
    }


}
