using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace ForwardingMessages
{
    class Program
    {
        internal class AddOneMore
        {
            public DateTime Birthday { get; private set; }

            public AddOneMore(DateTime birthday)
            {
                Birthday = birthday;
            }
        }

        internal class TodlerCount
        {
            public int Count { get; private set; }

            public TodlerCount(int todlerCount)
            {
                Count = todlerCount;
            }
        }


        internal class TeenCount
        {
            public int Count { get; private set; }

            public TeenCount(int todlerCount)
            {
                Count = todlerCount;
            }
        }

        public class TodlerActor : ReceiveActor
        {
            private int _todlersCount = 0;
            public TodlerActor()
            {
                Receive<AddOneMore>(m => { Sender.Tell(new TodlerCount(++_todlersCount)); });
            }
        }

        public class TeenActor : ReceiveActor
        {
            private int _teenCount = 0;
            public TeenActor()
            {
                Receive<AddOneMore>(m => { Sender.Tell(new TeenCount(++_teenCount)); });
            }
        }


        public class KidAgeRouterActor : ReceiveActor
        {
            private readonly IActorRef _todlerActor;
            private readonly IActorRef _teenActor;

            public KidAgeRouterActor()
            {
                _todlerActor = Context.ActorOf<TodlerActor>("todlerActor");
                _teenActor = Context.ActorOf<TeenActor>("teenActor");

                Receive<AddOneMore>(m => AddOneMoreHandler(m));
            }

            private void AddOneMoreHandler(AddOneMore addOneMore)
            {
                var now = DateTime.Now;
                var age = now.Subtract(addOneMore.Birthday).TotalDays / 365;
                if (age <= 4)
                {
                    _todlerActor.Forward(addOneMore);
                }
                else
                {
                    _teenActor.Forward(addOneMore);
                }

            }
        }


        public class SummaryActor : ReceiveActor
        {
            private readonly IActorRef _kidAgeRouterActor;

            public SummaryActor()
            {
                _kidAgeRouterActor = Context.ActorOf<KidAgeRouterActor>("kidAgeRouterActor");

                Receive<AddOneMore>(m =>
                {
                    _kidAgeRouterActor.Tell(m, Self);
                });

                Receive<TodlerCount>(m => Console.WriteLine("Current TodlerCount : {0} at DateTime.Now.Tiks : {1}", m.Count, DateTime.Now.Ticks));
                Receive<TeenCount>(m => Console.WriteLine("Current TeenCount : {0} at DateTime.Now.Tiks : {1}", m.Count, DateTime.Now.Ticks));
            }
        }

        static void Main(string[] args)
        {
            Console.Write("Demo for actor.Forward(message) as an alternative for Tell(msg, IActorRef)");
            var kidsCounterSystem = ActorSystem.Create("kidsCounter");
            var summaryActorProxy = kidsCounterSystem.ActorOf<SummaryActor>("summaryActor");
            summaryActorProxy.Tell(new AddOneMore(DateTime.Now.AddYears(-2)));
            summaryActorProxy.Tell(new AddOneMore(DateTime.Now.AddYears(-12)));
            summaryActorProxy.Tell(new AddOneMore(DateTime.Now.AddYears(-3)));
            summaryActorProxy.Tell(new AddOneMore(DateTime.Now.AddYears(-13)));
            summaryActorProxy.Tell(new AddOneMore(DateTime.Now.AddYears(-10)));
            summaryActorProxy.Tell(new AddOneMore(DateTime.Now.AddYears(-18)));
            summaryActorProxy.Tell(new AddOneMore(DateTime.Now.AddYears(-4)));
            summaryActorProxy.Tell(new AddOneMore(DateTime.Now.AddYears(-3)));
            Console.ReadLine();
        }
    }
}
