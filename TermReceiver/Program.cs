using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TermReceiver.Models;

var secrets = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

ConnectionFactory termReceiver = new();

termReceiver.Uri = new Uri($"amqp://{secrets["RabbitMqUsername"]}:{secrets["RabbitMqPassword"]}@localhost:5672");
termReceiver.ClientProvidedName = "Term Receiver";

IConnection connection = termReceiver.CreateConnection();
IModel channel = connection.CreateModel();

string exchangeName = "TermExchange";
string queueName = "TermReturnQueue";
string routingKey = "term.response.success";

channel.ExchangeDeclare(exchangeName, ExchangeType.Topic);
channel.QueueDeclare(queueName, false, false, false);
channel.QueueBind(queueName, exchangeName, routingKey);
channel.BasicQos(0, 1, false);

var consumer = new EventingBasicConsumer(channel);

Console.WriteLine("Term receiver ready");

consumer.Received += (sender, args) =>
{
    var body = args.Body.ToArray();

    // Console.WriteLine($"{args.RoutingKey}: {body}");

    string message = Encoding.UTF8.GetString(body);

    var term = JsonSerializer.Deserialize<Term>(message);

    Console.WriteLine($"Term received: {term.title}");

    term.SaveTerm();

    channel.BasicAck(args.DeliveryTag, false);
};

string consumerTag = channel.BasicConsume(queueName, false, consumer);

Console.ReadLine();

channel.BasicCancel(consumerTag);

channel.Close();
connection.Close();