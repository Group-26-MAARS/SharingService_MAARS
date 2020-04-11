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
    [Route("api/animations")]
    [ApiController]
    public class AnimationsController : ControllerBase
    {
        private readonly IAnimationKeyCache animationKeyCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimationsController"/> class.
    /// </summary>
    /// <param name="animationKeyCache">The animation key cache.</param>
    public AnimationsController(IAnimationKeyCache animationKeyCache)
    {
        this.animationKeyCache = animationKeyCache;
    }

    // GET api/animationName/animationName
    [HttpGet("{animationName}")]
    public async Task<ActionResult<string>> GetAsync(string animationName)
    {
        Console.WriteLine("Get Request in GetAsync()\n");
        // Get the key if present
        try
        {
            return await this.animationKeyCache.GetAnimationKeyAsync(animationName);
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound();
        }
    }
    // GET api/animation/all
    [HttpGet("all")]
    public async Task<ActionResult<List<string>>> GetAllAsync()
    {
        List<string> outputList = new List<string>();
        // Get the all animations
        List<AnimationCacheEntity> animationCacheEntityList = await this.animationKeyCache.GetAllAnimationKeysAsync();
        foreach (AnimationCacheEntity cacheEntity in animationCacheEntityList)
        {
            outputList.Add(cacheEntity.RowKey + " : " + cacheEntity.AnimationJSON);
        }

        return outputList;
    }

    // GET api/animations/remove/animationName
    [HttpGet("remove/{animationName}")]
    public async Task<ActionResult<string>> DeleteAsync(string animationName)
    {
        Console.WriteLine("Get Request in GetAsync()\n");
        // Get the key if present
        try
        {
            return await this.animationKeyCache.DeleteAnimationKeyAsync(animationName);
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound();
        }
    }

    /*
    // GET api/animations/allForRoute/myRouteName
    [HttpGet("allForRoute/{routeName}")]

    public async Task<ActionResult<List<string>>> GetAllForRouteAsync(string routeName)
    {
        List<string> outputList = new List<string>();

        // Get the all anchor caches for the anchors that are found in the given route
        string anchorIdents = await _routesController.routeKeyCache.GetRouteKeyAsync(routeName);
        string[] anchorNames = anchorIdents.Replace(" ", "").Split(','); // what should this be split on?
        foreach (string currStr in anchorNames)
        {
            string allAnchorsData = await this.anchorKeyCache.GetAnchorKeyAsync(currStr);
            outputList.Add(allAnchorsData);
        }

        return outputList;
    }
    */

    // POST api/animations
    [HttpPost]
    public async Task<ActionResult<string>> PostAsync()
    {
        string animationKey;
        string animationName;

        using (StreamReader reader = new StreamReader(this.Request.Body, Encoding.UTF8))
        {
            string tempStr = await reader.ReadToEndAsync();
            //tempStr = "someAnimationName:someAnimationSerializedJSON";
            animationName = tempStr.Split(":")[0];
            animationKey = tempStr.Split(":")[1];
        }

        if (string.IsNullOrWhiteSpace(animationKey))
        {
            return this.BadRequest();
        }

        // Set the key and return the animation number
        return await this.animationKeyCache.SetAnimationKeyAsync(animationName, animationKey);
    }
}
}
