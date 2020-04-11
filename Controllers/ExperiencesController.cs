// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.AspNetCore.Mvc;
using SharingService.Data;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace SharingService.Controllers
{
    [Route("api/experiences")]
    [ApiController]
    public class ExperiencesController : ControllerBase
    {

        private readonly IAnchorKeyCache anchorKeyCache;
        private readonly IRouteKeyCache routeKeyCache;
        private readonly IAnimationKeyCache animationKeyCache;
        private readonly RoutesController _routesController;
        private readonly AnchorsController _anchorsController;


        //private readonly IExperienceKeyCache experienceKeyCache;
        public IExperienceKeyCache experienceKeyCache;
        /// <summary>
        /// Initializes a new instance of the ExperiencesController class.
        /// </summary>
        /// <param name="experienceKeyCache">The experience key cache.</param>
        public ExperiencesController(IExperienceKeyCache experienceKeyCache, IAnimationKeyCache animationKeyCache, IAnchorKeyCache anchorKeyCache, AnchorsController anchorsController, RoutesController routesController)
        {
            this.experienceKeyCache = experienceKeyCache;
            this._routesController = routesController;
            this.anchorKeyCache = anchorKeyCache;
            this._anchorsController = anchorsController;
            this.animationKeyCache = animationKeyCache;
        }

        // GET api/experiences/5
        [HttpGet("{experienceName}")]
        public async Task<ActionResult<string>> GetAsync(string experienceName)
        {
            Console.WriteLine("Get Request in GetAsync()\n");
            // Get the key if present
            try
            {
                return await this.experienceKeyCache.GetExperienceKeyAsync(experienceName);
            }
            catch(KeyNotFoundException)
            {
                return this.NotFound();
            }
        }

        // GET api/experiences/allassociated/experienceName
        [HttpGet("allassociated/{experienceName}")]
        public async Task<ActionResult<string>> GetAllAssocAsync(string experienceName)
        {
            string experienceItemsString = "";
            Console.WriteLine("Get Request in GetAsync()\n");
            // Get the key if present
            try
            {
                // currExperienceItem will be like the following:
                //R_ThisRoute, A_MyExperience, R_FinalRoute

                //experienceItemsString = await this.experienceKeyCache.GetExperienceKeyAsync(experienceName);
                experienceItemsString = "R_ThisRoute, A_someAnimationName, R_FinalRoute";
                string finalReturnStr = "";
                for (int i = 0; i <= experienceItemsString.Count(x => x == ','); i++)
                {
                    string currExperienceItem = experienceItemsString.Replace(" ", "").Split(',')[i];
                    // If current Item is a route item
                    if (currExperienceItem.Split('_')[0].Equals("R"))
                    {
                        // Give header for the Name of the Route preceding "=>"
                        finalReturnStr += currExperienceItem + "`";
                        // Append the list of all anchors onto this routename
                        // Need to call GetAllForRouteAsync() from AnchorsController

                        // Get the all anchor caches for the anchors that are found in the given route
                        string anchorIdents = await _routesController.routeKeyCache.GetRouteKeyAsync(currExperienceItem.Split('_')[1]);
                        currExperienceItem = "";
                        string[] anchorNames = anchorIdents.Replace(" ", "").Split(','); // what should this be split on?

                        foreach (string currStr in anchorNames)
                        {
                            string allAnchorsData = await this.anchorKeyCache.GetAnchorKeyAsync(currStr);
                            currExperienceItem += allAnchorsData + ",";
                        }
                        finalReturnStr += currExperienceItem.Substring(0, currExperienceItem.Length - 1) + "&";
                        // 
                    }
                    else if (currExperienceItem.Split('_')[0].Equals("A"))
                    {
                        finalReturnStr += "A_" + currExperienceItem.Split('_')[1] + "~" +
                            await this.animationKeyCache.GetAnimationKeyAsync(currExperienceItem.Split('_')[1]) + "&";

                    }
                }
                return finalReturnStr.Substring(0, finalReturnStr.Length - 1);

            }
            catch (KeyNotFoundException)
            {
                return this.NotFound();
            }
        }

        // GET api/experiences/all
        [HttpGet("all")]
        public async Task<ActionResult<List<string>>> GetAllAsync()
        {
            List<string> outputList = new List<string>();
            // Get the all experiences
            List<ExperienceCacheEntity> experienceCacheEntityList = await this.experienceKeyCache.GetAllExperienceKeysAsync();
            foreach (ExperienceCacheEntity cacheEntity in experienceCacheEntityList)
            {
                outputList.Add(cacheEntity.RowKey.Substring(2, cacheEntity.RowKey.Split(':')[0].Length - 2) + " : " + cacheEntity.ExperienceValues);
            }

            return outputList;
        }
        // GET api/experiences/last
        [HttpGet("last")]
        public async Task<ActionResult<string>> GetAsync()
        {
            // Get the last experience
            string AnchorIdentifiers = await this.experienceKeyCache.GetLastExperienceKeyAsync();

            if (AnchorIdentifiers == null)
            {
                return "";
            }

            return AnchorIdentifiers;
        }
        
        // GET api/experiences/remove/experienceName4
        [HttpGet("remove/{experienceName}")]
        public async Task<ActionResult<string>> DeleteAsync(string experienceName)
        {
            Console.WriteLine("Get Request in GetAsync()\n");
            // Get the key if present
            try
            {
                return await this.experienceKeyCache.DeleteExperienceKeyAsync(experienceName);
            }
            catch (KeyNotFoundException)
            {
                return this.NotFound();
            }
        }
        

        // POST api/experiences
        [HttpPost]
        public async Task<ActionResult<string>> PostAsync()
        {
            string experienceName;
            string anchorIdentifiers;
            string subStr;
            using (StreamReader reader = new StreamReader(this.Request.Body, Encoding.UTF8))
            {
                subStr = await reader.ReadToEndAsync();
                subStr = "MyExperienceName:R_ThisRoute, A_MyExperience, R_FinalRoute";
                experienceName = subStr.Split(":")[0];
                anchorIdentifiers = subStr.Split(":")[1];
            }

            //experienceKey = "1";

            if (string.IsNullOrWhiteSpace(anchorIdentifiers) || string.IsNullOrWhiteSpace(experienceName))
            {
                return this.BadRequest();
            }

            // Set the key and return the experience number
            return await this.experienceKeyCache.SetExperienceKeyAsync(experienceName, anchorIdentifiers);
        }
    }
}
