using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

using Akka;
using Akka.Actor;
using Akka.Pattern;
using Newtonsoft.Json;

namespace FirstActor
{


    public class MyActor : ReceiveActor
    {
        public MyActor()
        {
            Receive<SomeMessage>(m => HandleSomeMessage(m));

            Receive<string>(m =>
            {
                Console.WriteLine(m);

                //sender will not be know for Tell but only for Ask or Send(through the Inbox)
                Sender.Tell(String.Format("world : {0}", DateTime.Now));

                //an attempt to schedule the send - works if invokation was made through Send or Ask but not for Tell -> goes to DeadLetters
                Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1), Sender, String.Format("s'up at {0}", DateTime.Now), Self);


            });



            Receive<PingMessage>(m =>
            {

                Console.WriteLine("PingMessage.PingSendDateTime received : {0}", m.PingSendDateTime);
                Sender.Tell(String.Format("Ping is : PingSendDateTime : {0} - DateTime.Now.Ticks : {1}", m.PingSendDateTime.Ticks, DateTime.Now.Ticks));
            });

        }

        private void HandleSomeMessage(SomeMessage someMessage)
        {
            Console.WriteLine(someMessage.Info);
             
        }
    }

    public class SomeMessage
    {

        public string Info
        {
            get;
            private set;
        }

        public SomeMessage(string theInformation)
        {
            this.Info = theInformation;
        }
    }

    public class PingMessage
    {
        public DateTime PingSendDateTime { get; private set; }

        public PingMessage(DateTime pingSendDateTime)
        {
            PingSendDateTime = pingSendDateTime;
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            
            var actorSystem = ActorSystem.Create("MyActorSystem");

            Console.WriteLine("Demo for Tell");
            var myActorProxy = actorSystem.ActorOf<MyActor>("myActorInstance");
            myActorProxy.Tell(new SomeMessage("Hello World"));

            Console.ReadLine();



            Console.WriteLine("Demo for Inbox.Send - response is expected");
            
            var inbox = Inbox.Create(actorSystem);

            inbox.Send(myActorProxy, new PingMessage(DateTime.Now));

            try
            {
                var received = inbox.Receive(TimeSpan.FromSeconds(1));
                Console.WriteLine("Receiving response message '{0}' at : DateTime.Now.Ticks : {1}", received, DateTime.Now.Ticks);

                 




                


            }
            catch (TimeoutException ti)
            {
                Console.WriteLine(JsonConvert.SerializeObject(ti));
            }

             Console.ReadLine();


            try
            {
                Console.WriteLine("Demo for Ask and Wait with some timeout - response is espected within the timeout or exception");
                var task = myActorProxy.Ask(new PingMessage(DateTime.Now));
                task.Wait(TimeSpan.FromSeconds(2));
                Console.WriteLine("Receiving response message '{0}' at : DateTime.Now.Ticks : {1}", task.Result, DateTime.Now.Ticks);
            }
            catch (TimeoutException ti)
            {
                Console.WriteLine(JsonConvert.SerializeObject(ti));
            }

            Console.ReadLine();






            Console.WriteLine("An attempt to schedule the recieving in on regular intervals - with inbox!");
            inbox.Send(myActorProxy, "init message");
            actorSystem.Scheduler.Advanced.ScheduleRepeatedly(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1), () =>
            {
                var rec = inbox.Receive(TimeSpan.FromSeconds(10));
                Console.WriteLine("message '{0}' received at : {1}", rec, DateTime.Now);
            });

            Console.ReadLine();


            
            Console.WriteLine("Inbox.Watch to receive the termination event of the watched actor");
            var thePillRecipientProxy = actorSystem.ActorOf<MyActor>("thePillRecipient");
            inbox.Watch(thePillRecipientProxy);
            thePillRecipientProxy.Tell(PoisonPill.Instance);
            var terminationMessage = inbox.Receive(TimeSpan.FromSeconds(1));
            Console.WriteLine("terminated '{0}' at : {1}", terminationMessage, DateTime.Now);
            Console.ReadLine();
        }

        

    }
}
