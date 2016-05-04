using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Newtonsoft.Json;

namespace ActorTypes
{
    class Program
    {

        public class StringsWithHanlerPriorityActor : ReceiveActor
        {
            public StringsWithHanlerPriorityActor()
            {
                Receive<string>(c => Console.WriteLine("I received : {0}", c));
                Receive<string>(c => Console.WriteLine("I will never be invoked in this actor : {0}", c));
            }
        }


        public class ReceiveStringsWithPredicates : ReceiveActor
        {
            public ReceiveStringsWithPredicates()
            {
                //this will handle all the strings that contain at least one digit
                Receive<string>(c => c.Any(ch => char.IsDigit(ch)), c => Console.WriteLine("{0} - handled by handler with a predicate", c));
                Receive<string>(c => Console.WriteLine("{0} - punctuation predicate ", c), c => c.Any(char.IsPunctuation));
                

                //sample for Func<T,bool> in the role of a both predicate and handler
                Receive<string>(c =>
                {
                    if (c.Contains("go"))
                    {
                        Console.WriteLine("{0} - GO handler handled this",c );
                        return true;
                    }
                    return false;
                });


                Receive<string>(c => Console.WriteLine("{0} - no predicate", c));

            }


            protected override void Unhandled(object message)
            {
                Console.WriteLine("I do not know what to do with this instance of type : {0}", message.GetType());
            }

        }


        public class ForIntsAndAny : ReceiveActor
        {
            public ForIntsAndAny()
            {
                Receive<int>(m => Console.WriteLine("{0} is int", m));
                ReceiveAny(m=>Console.WriteLine("I got instance of type : {0}", m.GetType()));
            }
        }



        public class MyUntypedActor : UntypedActor
        {
            protected override void OnReceive(object message)
            {
                Console.WriteLine("I got a {0} and it looks like : {1}", message.GetType(),
                    JsonConvert.SerializeObject(message));
            }
        }




        public class MyTypedActor : TypedActor, IHandle<string>
        {
            public void Handle(string message)
            {
                Console.WriteLine("{0} - was the message string", message);
            }
        }




        public class MyActorBase : ActorBase
        {
            protected override bool Receive(object message)
            {
                Console.WriteLine("In the ActorBase, I got a {0} and it looks like : {1}", message.GetType(),
                    JsonConvert.SerializeObject(message));
                return true;
            }
        }


        public class MyPredicateLikeActorBase : ActorBase
        {
            protected override bool Receive(object message)
            {
                if (message.GetType().Equals(typeof(string)))
                {
                    Console.WriteLine("In the ActorBase, I got a {0} and it looks like : {1}", message.GetType(),
                    JsonConvert.SerializeObject(message));
                    return true;
                }


                if (message.GetType() == typeof(StringBuilder))
                {
                    Console.WriteLine("In the ActorBase, I got a {0} and it looks like : {1}", message.GetType(),
                    JsonConvert.SerializeObject(
                    (message as StringBuilder).ToString()
                    ));
                    return true;
                }
                
                Console.WriteLine("I WILL NOT HANDLE THIS, I got a {0} and it looks like : {1}", message.GetType(),
                    JsonConvert.SerializeObject(message));
                return false;
            }
        }



        static void Main(string[] args)
        {


            Console.WriteLine("ReceiveActor - handler priority - in order in which the handlers are defined");
            var actorSystem = ActorSystem.Create("myWay");
            var stringsWithHanlerPriorityActorProxy = actorSystem.ActorOf<StringsWithHanlerPriorityActor>("stringsWithHanlerPriorityActor");
            stringsWithHanlerPriorityActorProxy.Tell("Yo!");

            Console.ReadLine();


            Console.WriteLine("Demo for ReceiveActor with predicates for filtering inputs among strings that contain numbers and those that dont");
            var receiveStringsWithPredicatesProxy = actorSystem.ActorOf<ReceiveStringsWithPredicates>("predicateStringSeleector");
            receiveStringsWithPredicatesProxy.Tell("Alo!");
            receiveStringsWithPredicatesProxy.Tell("I have 1 wish");
            receiveStringsWithPredicatesProxy.Tell("My . is :");
            receiveStringsWithPredicatesProxy.Tell("Give it a go");

            receiveStringsWithPredicatesProxy.Tell(new StringBuilder("init string"));

            Console.ReadLine();


            Console.WriteLine("Demo for ReceiveAny");
            var forIntsAndAnyProxy = actorSystem.ActorOf<ForIntsAndAny>("forIntsAndAllOtherThings");
            forIntsAndAnyProxy.Tell(123);
            forIntsAndAnyProxy.Tell("Hello!");
            Console.ReadLine();


            Console.WriteLine("Demo for UntypedActor - implements only OnReceive(object message)");
            var untypedActorProxy = actorSystem.ActorOf<MyUntypedActor>("untypedActor");
            untypedActorProxy.Tell("this is some sample string");
            untypedActorProxy.Tell(456);
            untypedActorProxy.Tell(new StringBuilder("trala-la"));
            Console.ReadLine();


            Console.WriteLine("Demo for the TypedActor");
            var typedActorProxy = actorSystem.ActorOf<MyTypedActor>("typedActor");
            typedActorProxy.Tell("Hi");
            typedActorProxy.Tell(123);
            typedActorProxy.Tell(new StringBuilder("trala-la"));
            Console.ReadLine();




            Console.WriteLine("Demo for ActorBase - implements only Receive(object message)");
            var actorBaseActorProxy = actorSystem.ActorOf<MyUntypedActor>("actorBaseActor");
            actorBaseActorProxy.Tell("sample string for the ActorBase");
            actorBaseActorProxy.Tell(789);
            actorBaseActorProxy.Tell(new StringBuilder("bla bla 1232123"));
            Console.ReadLine();




            Console.WriteLine("Demo for ActorBase with inspection of the message - implements only Receive(object message) and inspects the received type");
            var actorPredicateBaseActorProxy = actorSystem.ActorOf<MyPredicateLikeActorBase>("actorPredicateBaseActor");
            actorPredicateBaseActorProxy.Tell("sample string for the predicate ActorBase");
            actorPredicateBaseActorProxy.Tell(1112);
            actorPredicateBaseActorProxy.Tell(new StringBuilder("hm, something fishy..."));
            Console.ReadLine();


        }
    }
}
