namespace RabbitMQPlayground.Routing
{
    public interface IEventSerializer
    {
        string GetSubject(IEvent @event);
    }
}