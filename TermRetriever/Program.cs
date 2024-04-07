using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TermRetriever.Models;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Threading.Channels;

var secrets = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

ConnectionFactory termRequestReceiver = new ConnectionFactory() { DispatchConsumersAsync = true};

termRequestReceiver.Uri = new Uri($"amqp://{secrets["RabbitMqUsername"]}:{secrets["RabbitMqPassword"]}@localhost:5672");
termRequestReceiver.ClientProvidedName = "Term Returner";

IConnection receiverConnection = termRequestReceiver.CreateConnection();
IModel receiverChannel = receiverConnection.CreateModel();

string exchangeName = "TermRequestExchange";
string queueName = "TermRequestQueue";
string routingKey = "term.request";

receiverChannel.ExchangeDeclare(exchangeName, ExchangeType.Direct);
receiverChannel.QueueDeclare(queueName, false, false, false);
receiverChannel.QueueBind(queueName, exchangeName, routingKey);
receiverChannel.BasicQos(0, 1, false);

var consumer = new AsyncEventingBasicConsumer(receiverChannel);

Console.WriteLine("Term retriever ready");

consumer.Received += async (sender, args) =>
{
    var body = args.Body.ToArray();

    string requestTerm = Encoding.UTF8.GetString(body);

    Console.WriteLine($"Received request for: {requestTerm}");

    receiverChannel.BasicAck(args.DeliveryTag, false);

    Term term = new();

    await term.GetTerm(requestTerm);

    if(term.title != null)
    {
        Console.WriteLine($"Returning term details for: {requestTerm}");
        term.ReturnTerm(true, requestTerm);
    }
    else
    {
        Console.WriteLine($"Returning failure for: {requestTerm}");
        term.ReturnTerm(false);
    }
};

string receiverConsumerTag = receiverChannel.BasicConsume(queueName, false, consumer);

Console.ReadLine();

receiverChannel.BasicCancel(receiverConsumerTag);

receiverChannel.Close();
receiverConnection.Close();