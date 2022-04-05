using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace ProtectedResource.Lib.Services
{
	public class MessagingQueueService
		: IMessagingQueueService
	{
		public delegate void JsonQueueItem(string json);

		private IConnection _connection;
		private IModel _channel;
		private EventingBasicConsumer _consumer;
		private JsonQueueItem _onProcessQueue;

		public MessagingQueueService()
		{
			//TODO: Inject configuration
		}

		public void Start(string queueName, JsonQueueItem processQueueItem)
		{
			_onProcessQueue = processQueueItem;

			//Read items from the queue
			_connection = GetConnection();

			_channel = _connection.CreateModel();
			_channel.QueueDeclare(
				queue: queueName,
				durable: false,
				exclusive: false,
				autoDelete: false,
				arguments: null);

			_consumer = new EventingBasicConsumer(_channel);
			_consumer.Received += ProcessQueue;

			_channel.BasicConsume(queue: queueName,
				autoAck: true,
				consumer: _consumer);
		}

		private IConnection GetConnection()
		{
			//TODO: Make hostname configurable
			var factory = new ConnectionFactory() { HostName = "localhost" };

			var con = factory.CreateConnection();

			return con;
		}

		private void ProcessQueue(object model, BasicDeliverEventArgs args)
		{
			var body = args.Body.ToArray();

			var json = Encoding.UTF8.GetString(body);

			_onProcessQueue(json);
		}

		public void Dispose()
		{
			_channel?.Dispose();

			_connection?.Dispose();
		}
	}
}
