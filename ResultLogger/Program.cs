using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

var secrets = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

ConnectionFactory resultLogger = new ConnectionFactory
{
    HostName = "localhost",
    UserName = secrets["RabbitMqUsername"],
    Password = secrets["RabbitMqPassword"],
    ClientProvidedName = "Result Logger"
};

IConnection connection = resultLogger.CreateConnection();
IModel channel = connection.CreateModel();

string exchangeName = "TermExchange";
string queueName = "TermResultLogger";
string routingKey = "term.response.*";

channel.ExchangeDeclare(exchangeName, ExchangeType.Topic);
channel.QueueDeclare(queueName, false, false, false);
channel.QueueBind(queueName, exchangeName, routingKey);
channel.BasicQos(0, 1, false);

var consumer = new EventingBasicConsumer(channel);

Console.WriteLine("Result logger ready");

consumer.Received += (sender, args) =>
{
    var body = args.Body.ToArray();

    string message = Encoding.UTF8.GetString(body);

    Console.WriteLine($"{args.RoutingKey}: {message}");

    channel.BasicAck(args.DeliveryTag, false);
};

string consumerTag = channel.BasicConsume(queueName, false, consumer);

Console.ReadLine();

channel.BasicCancel(consumerTag);

channel.Close();
connection.Close();