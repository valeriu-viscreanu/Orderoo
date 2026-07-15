namespace OrderApi.Handlers
{
    public interface IQueryHandler<in TQuery, TResult>
   
    {
        Task<TResult?> Handle(
            TQuery query,
            CancellationToken cancellationToken = default);
    }
}
