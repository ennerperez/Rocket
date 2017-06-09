﻿using System;

namespace Rocket.API.Providers
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class RocketProviderAttribute : Attribute
    {
        public bool SupportsMultiple { get; set; } = true;
    }
}