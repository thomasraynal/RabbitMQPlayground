namespace RabbitMQPlayground.Routing
{
    public interface IEventSerializer
    {
        ISerializer Serializer { get; }
        string GetSubject(IEvent @event);
    }
}