// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.AspNetCore.Mvc;
using SharingService.Data;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
namespace SharingService.Controllers
{
    [Route("api/routes")]
    [ApiController]
    public class RoutesController : ControllerBase
    {
        //private readonly IRouteKeyCache routeKeyCache;
        public IRouteKeyCache routeKeyCache;
        /// <summary>
        /// Initializes a new instance of the RoutesController class.
        /// </summary>
        /// <param name="routeKeyCache">The route key cache.</param>
        public RoutesController(IRouteKeyCache routeKeyCache)
        {
            this.routeKeyCache = routeKeyCache;
        }

        // GET api/routes/5
        [HttpGet("{routeName}")]
        public async Task<ActionResult<string>> GetAsync(string routeName)
        {
            Console.WriteLine("Get Request in GetAsync()\n");
            // Get the key if present
            try
            {
                return await this.routeKeyCache.GetRouteKeyAsync(routeName);
            }
            catch(KeyNotFoundException)
            {
                return this.NotFound();
            }
        }
        // GET api/routes/all
        [HttpGet("all")]
        public async Task<ActionResult<List<string>>> GetAllAsync()
        {
            List<string> outputList = new List<string>();
            // Get the all routes
            List<RouteCacheEntity> routeCacheEntityList = await this.routeKeyCache.GetAllRouteKeysAsync();
            foreach (RouteCacheEntity cacheEntity in routeCacheEntityList)
            {
                outputList.Add(cacheEntity.RowKey + " : " + cacheEntity.AnchorIdentifiers);
            }

            return outputList;
        }
        // GET api/routes/last
        [HttpGet("last")]
        public async Task<ActionResult<string>> GetAsync()
        {
            // Get the last route
            string AnchorIdentifiers = await this.routeKeyCache.GetLastRouteKeyAsync();

            if (AnchorIdentifiers == null)
            {
                return "";
            }

            return AnchorIdentifiers;
        }
        
        // GET api/routes/remove/routeName4
        [HttpGet("remove/{routeName}")]
        public async Task<ActionResult<string>> DeleteAsync(string routeName)
        {
            Console.WriteLine("Get Request in GetAsync()\n");
            // Get the key if present
            try
            {
                return await this.routeKeyCache.DeleteRouteKeyAsync(routeName);
            }
            catch (KeyNotFoundException)
            {
                return this.NotFound();
            }
        }
        

        // POST api/routes
        [HttpPost]
        public async Task<ActionResult<string>> PostAsync()
        {
            string routeName;
            string anchorIdentifiers;
            string subStr;
            using (StreamReader reader = new StreamReader(this.Request.Body, Encoding.UTF8))
            {
                subStr = await reader.ReadToEndAsync();
                //subStr = "YetAnotherRouteName:24, 28, 33";
                routeName = subStr.Split(":")[0];
                anchorIdentifiers = subStr.Split(":")[1];
            }

            //routeKey = "1";

            if (string.IsNullOrWhiteSpace(anchorIdentifiers) || string.IsNullOrWhiteSpace(routeName))
            {
                return this.BadRequest();
            }

            // Set the key and return the route number
            return await this.routeKeyCache.SetRouteKeyAsync(routeName, anchorIdentifiers);
        }
    }
}
