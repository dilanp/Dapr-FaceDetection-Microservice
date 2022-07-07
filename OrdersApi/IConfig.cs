namespace OrdersApi
{
    public interface IConfig
    {
        bool RunDbMigrations { get; set; }

    }
}
