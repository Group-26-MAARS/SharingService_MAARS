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
    [Route("api/anchors")]
    [ApiController]
    public class AnchorsController : ControllerBase
    {
        private readonly IAnchorKeyCache anchorKeyCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnchorsController"/> class.
        /// </summary>
        /// <param name="anchorKeyCache">The anchor key cache.</param>
        public AnchorsController(IAnchorKeyCache anchorKeyCache)
        {
            this.anchorKeyCache = anchorKeyCache;
        }

        // GET api/anchors/5
        [HttpGet("{anchorNumber}")]
        public async Task<ActionResult<string>> GetAsync(long anchorNumber)
        {
            Console.WriteLine("Get Request in GetAsync()\n");
            // Get the key if present
            try
            {
                return await this.anchorKeyCache.GetAnchorKeyAsync(anchorNumber);
            }
            catch(KeyNotFoundException)
            {
                return this.NotFound();
            }
        }
        // GET api/anchors/all
        [HttpGet("all")]
        public async Task<ActionResult<List<string>>> GetAllAsync()
        {
            List<string> outputList = new List<string>();
            // Get the all anchors
            List<AnchorCacheEntity> anchorCacheEntityList = await this.anchorKeyCache.GetAllAnchorKeysAsync();
            foreach (AnchorCacheEntity cacheEntity in anchorCacheEntityList)
            {
                outputList.Add(cacheEntity.RowKey + " : " + cacheEntity.AnchorKey);
            }

            return outputList;
        }
        // GET api/anchors/last
        [HttpGet("last")]
        public async Task<ActionResult<string>> GetAsync()
        {
            // Get the last anchor
            string anchorKey = await this.anchorKeyCache.GetLastAnchorKeyAsync();

            if (anchorKey == null)
            {
                return "";
            }

            return anchorKey;
        }

        // POST api/anchors
        [HttpPost]
        public async Task<ActionResult<long>> PostAsync()
        {
            string anchorKey;
            string anchorName;
            string location;
            string expiration;
            string description;
            string tempStr;

            using (StreamReader reader = new StreamReader(this.Request.Body, Encoding.UTF8))
            {
                tempStr = await reader.ReadToEndAsync();
                //tempStr = "40kjl3kht:MyNewestAnchor:AtUCF:12-20-20:this is the first anchor in the newer form";
                anchorKey = tempStr.Split(":")[0];
                anchorName = tempStr.Split(":")[1];
                location = tempStr.Split(":")[2];
                expiration = tempStr.Split(":")[3];
                description = tempStr.Split(":")[4];

            }

            //anchorKey = "1";

            if (string.IsNullOrWhiteSpace(anchorKey))
            {
                return this.BadRequest();
            }

            // Set the key and return the anchor number
            return await this.anchorKeyCache.SetAnchorKeyAsync(anchorKey, anchorName, location, expiration, description);
        }
    }
}
