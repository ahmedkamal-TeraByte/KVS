using KVS.Structure;
using KVS.Structure.Models;
using Microsoft.AspNetCore.Mvc;

namespace KVS.Api.Controllers
{
    [Route("kv")]
    [ApiController]
    public class KeyValueStoreController : ControllerBase
    {
        private readonly IStore _store;
        private readonly ILogger<KeyValueStoreController> _logger;

        public KeyValueStoreController(IStore store, ILogger<KeyValueStoreController> logger)
        {
            _store = store;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ICollection<string>>> Get()
        {
            var value = await _store.GetAllKeysAsync();
            if (value is not { Count: > 0 })
                return NotFound();
            return Ok(value);
        }

        [HttpGet]
        [Route("{key}")]
        public async Task<ActionResult<StoreValue>> Get(string key)
        {
            var value = await _store.GetAsync(key);
            if (value == null)
                return NotFound();
            return Ok(value);
        }

        [HttpPut]
        [Route("{key}")]
        public async Task<ActionResult<StoreValue>> Put(string key, [FromBody] string value,
            [FromQuery] int ifVersion = 0)
        {
            try
            {
                var result = await _store.PutAsync(key, value, ifVersion);
                return result;
            }
            catch (ArgumentNullException e)
            {
                _logger.LogError(e.Message);
                return BadRequest(e.Message);
            }
            catch (InvalidOperationException)
            {
                return Conflict();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPatch]
        [Route("{key}")]
        public async Task<ActionResult<StoreValue>> Patch(string key, [FromBody] string value,
            [FromQuery] int ifVersion = 0)
        {
            try
            {
                var result = await _store.PatchAsync(key, value, ifVersion);
                return result;
            }
            catch (ArgumentNullException e)
            {
                _logger.LogError(e.Message);
                return BadRequest(e.Message);
            }
            catch (InvalidOperationException)
            {
                return Conflict();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}