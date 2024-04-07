using RabbitMQ.Client;
using System.Text;
using Microsoft.Extensions.Configuration;

var secrets = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

ConnectionFactory termRequester = new();

termRequester.Uri = new Uri($"amqp://{secrets["RabbitMqUsername"]}:{secrets["RabbitMqPassword"]}@localhost:5672");
termRequester.ClientProvidedName = "Term Requester";

IConnection cnn = termRequester.CreateConnection();
IModel channel = cnn.CreateModel();

string exchangeName = "TermRequestExchange";
string queueName = "TermRequestQueue";
string routingKey = "term.request";

channel.ExchangeDeclare(exchangeName, ExchangeType.Direct);
channel.QueueDeclare(queueName, false, false, false);
channel.QueueBind(queueName, exchangeName, routingKey);

string? requestTerm = null;

while(requestTerm != "exit")
{
    Console.WriteLine("Specify term");
    requestTerm = Console.ReadLine();

    Console.WriteLine();
    Console.WriteLine($"Requesting term: {requestTerm}");
    Console.WriteLine();
    Console.WriteLine();

    byte[] messageBodyBytes = Encoding.UTF8.GetBytes(requestTerm);
    channel.BasicPublish(exchangeName, routingKey, null, messageBodyBytes);
}

channel.Close();
cnn.Close();