using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace TermRetriever.Models
{
    public class Term
    {
        public string? id { get; set; }
        public string? term { get; set; }
        public string? title { get; set; }
        public string? plural { get; set; }
        public DateOnly? created { get; set; }
        public DateOnly? updated { get; set; }
        public string? @class { get; set; }
        public string? gender { get; set; }

        public async Task GetTerm(string requestTerm)
        {
            HttpClient client = new();

            var stream = client.GetStreamAsync($"https://panzerworld.com/german-military-dictionary/api/?term={requestTerm}");

            try
            {
                var result = await JsonSerializer.DeserializeAsync<Term>(await stream);

                id = result.id;
                term = result.term;
                title = result.title;
                plural = result.plural;
                created = result.created;
                updated = result.updated;
                @class = result.@class;
                gender = result.gender;

                Console.WriteLine("Term found!");
                Console.WriteLine($"Term..........: {result.term}");
                Console.WriteLine($"Title.........: {result.title}");
                Console.WriteLine($"Plural........: {result.plural}");
                Console.WriteLine($"Created.......: {result.created}");
                Console.WriteLine($"Updated.......: {result.updated}");
                Console.WriteLine($"Class.........: {result.@class}");
                Console.WriteLine($"Gender........: {result.gender}");
            }
            catch
            {
                Console.WriteLine("Term not found!");
            }
        }

        public void ReturnTerm(bool success, string key = null)
        {
            var secrets = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

            ConnectionFactory factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = secrets["RabbitMqUsername"],
                Password = secrets["RabbitMqPassword"],
                ClientProvidedName = "Term Returner"
            };

            IConnection connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();

            string exchangeName = "TermExchange";

            channel.ExchangeDeclare(exchangeName, ExchangeType.Topic);

            string routingKey;
            string message;

            if(success)
            {
                routingKey = "term.response.success";
                message = JsonSerializer.Serialize(this);
            }
            else
            {
                routingKey = "term.response.failure";
                message = $"Wrong term: {key}";
            }
            
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchangeName, routingKey, null, body);

            channel.Close();
            connection.Close();
        }
    }
}
