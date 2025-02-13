using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "spiderman",
    Password = "aranhaverso"
};
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.QueueDeclare("task_queue",
    true,
    false,
    false,
    null);

var message = GetMessage(args);
var body = Encoding.UTF8.GetBytes(message);
channel.BasicPublish(string.Empty,
    "hello",
    null,
    body);
Console.WriteLine($" [x] Sent {message}");

// Console.WriteLine(" Press [enter] to exit.");
// Console.ReadLine();

static string GetMessage(string[] args)
{
    return args.Length > 0 ? string.Join(" ", args) : "Hello World!";
}