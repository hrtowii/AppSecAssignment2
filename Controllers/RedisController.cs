using StackExchange.Redis;

namespace Assignment2.Services
{
    public class RedisService
    {
        private readonly ConnectionMultiplexer _redis;

        public RedisService(IConfiguration configuration)
        {
            // var redisConnectionString = configuration.GetConnectionString("Redis");
            // https://stackexchange.github.io/StackExchange.Redis/Configuration.html
            _redis = ConnectionMultiplexer.Connect("localhost");
        }

        public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(key, value, expiry);
        }

        public async Task<string> GetAsync(string key)
        {
            var db = _redis.GetDatabase();
            return await db.StringGetAsync(key);
        }

        public async Task DeleteAsync(string key)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
    }
}