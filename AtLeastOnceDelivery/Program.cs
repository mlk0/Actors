using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence;
using Newtonsoft.Json;

//using Akka.Persistence;

namespace AtLeastOnceDelivery
{


     public class RequestCommand
    {
         public readonly IActorRef SendTo;
         public string TheRequestCommandMessage { get; private set; }
         


         public RequestCommand(string theRequestCommandMessage, IActorRef sendTo)
         {
             SendTo = sendTo;
             TheRequestCommandMessage = theRequestCommandMessage;
         }
    }


    public class SomeMessage
    {
        public long DeliveryId { get; private set; }
        public string MessageContent { get; private set; }
        public ActorPath RequestActorPath { get; private set; }
        public SomeMessage(long deliveryId, string messageContent, ActorPath requestActorPath)
        {
            Console.WriteLine("SomeMessage created with deliveryId : {0} and messageContent : {1}", deliveryId, messageContent);
            DeliveryId = deliveryId;
            MessageContent = messageContent;
            RequestActorPath = requestActorPath;
        }
        
    }


    public class ConfirmMessage
    {
        public long DeliveryId { get; private set; }
        public ActorPath RequestActorPath { get; private set; }
        public ConfirmMessage(long deliveryId, ActorPath requestActorPath)
        {
            Console.WriteLine("Construction ConfirmMessage with deliveryId : {0}", deliveryId);
            DeliveryId = deliveryId;
            RequestActorPath = requestActorPath;
        }
    }


    public class MessageSentEvent
    {
        public string Message { get; private set; }
        public ActorPath RequestActorPath { get; set; }

        public MessageSentEvent(string message, ActorPath requestActorPath)
        {
            Message = message;
            RequestActorPath = requestActorPath;
        }
    }

    public class MessageDeliveredEvent
    {
        public long DeliveryId { get; private set; }
        public ActorPath RequestActorPath { get; set; }
        public MessageDeliveredEvent(long deliveryId, ActorPath requestActorPath)
        {
            DeliveryId = deliveryId;
            RequestActorPath = requestActorPath;
        }
    }


    
   

    public class MyFirstAtLeastOnceDeliveryActor : AtLeastOnceDeliveryReceiveActor
    {
        private const string MyFirstAtLeastOnceDeliveryActorId = "123ABC";
        public override string PersistenceId {
            get { return MyFirstAtLeastOnceDeliveryActorId; }
        }
        

        readonly IActorRef _restClinetActorProxy = Context.ActorOf<RestClientActor>("restClientActor");



        public MyFirstAtLeastOnceDeliveryActor()
        {
            Command<RequestCommand>(requestCommand => HandlerRequestCommand(requestCommand));
            Command<ConfirmMessage>(confirmMessageCommand => HandleConfirmMessageCommand(confirmMessageCommand));


            Recover<MessageSentEvent>(messageSentEvent => MessageSentEventHandler(messageSentEvent));
            Recover<MessageDeliveredEvent>(messageDeliveredEvent => MessageDeliveredEventHandler(messageDeliveredEvent));
        }


        private void HandlerRequestCommand(RequestCommand requestCommand)
        {
            var messageSentEvent = new MessageSentEvent(requestCommand.TheRequestCommandMessage, requestCommand.SendTo.Path);

            Persist(messageSentEvent, messageSentEventPersistCallback => MessageSentEventHandler(messageSentEventPersistCallback));
        }

        private void MessageSentEventHandler(MessageSentEvent messageSentEvent)
        {
            Deliver(_restClinetActorProxy.Path, deliveryId => new SomeMessage(deliveryId, messageSentEvent.Message, messageSentEvent.RequestActorPath));
        }




        
        private void HandleConfirmMessageCommand(ConfirmMessage confirmMessageCommand)
        {
            //ConfirmDelivery(confirmMessageCommand.DeliveryId);
            var messageConfirmedEvent = new MessageDeliveredEvent(confirmMessageCommand.DeliveryId, confirmMessageCommand.RequestActorPath);

            Persist(messageConfirmedEvent, messageConfirmedEventPersistCallback => MessageDeliveredEventHandler(messageConfirmedEventPersistCallback));
        }

        private void MessageDeliveredEventHandler(MessageDeliveredEvent messageConfirmedEventPersistCallback)
        {
            ConfirmDelivery(messageConfirmedEventPersistCallback.DeliveryId);
            var requestActor = Context.ActorSelection(messageConfirmedEventPersistCallback.RequestActorPath);
            //requestActor.Tell(messageConfirmedEventPersistCallback, Self);

            
            requestActor.Tell(messageConfirmedEventPersistCallback);
        }
    }



    public class RestClientActor : ReceiveActor
    {
        public RestClientActor()
        {
            Receive<SomeMessage>(c => SomeMessageHandler(c));
        }

        private void SomeMessageHandler(SomeMessage someMessage)
        {

            Console.WriteLine(String.Format("Received someMessage : '{0}'", someMessage));

            //do the rest call and if succeededs, send back the confirmation message
            Sender.Tell(new ConfirmMessage(someMessage.DeliveryId, someMessage.RequestActorPath), Self);
        }
    }



    public class ReqActor : ReceiveActor
    {
        public ReqActor()
        {
            Receive<RequestCommand>(c =>
            {
                var a = Context.ActorOf<MyFirstAtLeastOnceDeliveryActor>("fda");
                a.Tell(c, Self);
            });

            Receive<MessageDeliveredEvent>(c =>
            {

                Console.WriteLine(String.Format("the result : {0}", JsonConvert.SerializeObject(c.DeliveryId)));
                Sender.Tell(c);
            });
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var actorSystem = ActorSystem.Create("atLeastOnceDeliverySystem");
            var myFirstAtLeastOnceDeliveryActorProxy = actorSystem.ActorOf<MyFirstAtLeastOnceDeliveryActor>("myAtLeastFirstDeliveryActor");
            //var task = myFirstAtLeastOnceDeliveryActorProxy.Ask<MessageDeliveredEvent>(new RequestCommand("this is my message", myFirstAtLeastOnceDeliveryActorProxy), TimeSpan.FromSeconds(300));
            var task = myFirstAtLeastOnceDeliveryActorProxy.Ask(new RequestCommand(String.Format("this is my message at : {0}", DateTime.Now), myFirstAtLeastOnceDeliveryActorProxy),
                TimeSpan.FromSeconds(300));
            task.Wait();


            //var reqActProxy = actorSystem.ActorOf<MyFirstAtLeastOnceDeliveryActor>("reqActor");
            //var task = reqActProxy.Ask(String.Format("this is my message at : {0}", DateTime.Now));
            //task.Wait();
            var result = task.Result as MessageDeliveredEvent;


            Console.WriteLine(String.Format("the result : {0}", JsonConvert.SerializeObject(result)));
        }
    }
}
