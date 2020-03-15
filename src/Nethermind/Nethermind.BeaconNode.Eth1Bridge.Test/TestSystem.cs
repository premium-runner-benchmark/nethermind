﻿//  Copyright (c) 2018 Demerzel Solutions Limited
//  This file is part of the Nethermind library.
// 
//  The Nethermind library is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  The Nethermind library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Nethermind.BeaconNode.Storage;
using Nethermind.Core2.Configuration;
using Nethermind.Core2.Cryptography;

namespace Nethermind.BeaconNode.Eth1Bridge.Test
{
    public static class TestSystem
    {
        public static IServiceCollection BuildTestServiceCollection(IDictionary<string, string>? overrideConfiguration = null)
        {
            var services = new ServiceCollection();

            var inMemoryConfiguration = new Dictionary<string, string>
            {
                ["Peering:Mothra:LogSignedBeaconBlockJson"] = "false",
                ["Storage:InMemory:LogBlockJson"] = "false",
                ["Storage:InMemory:LogBlockStateJson"] = "false"
            };
            if (overrideConfiguration != null)
            {
                foreach (var kvp in overrideConfiguration)
                {
                    inMemoryConfiguration[kvp.Key] = kvp.Value;
                }
            }
            
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("Development/appsettings.json")
                .AddInMemoryCollection(inMemoryConfiguration)
                .Build();
            services.AddSingleton<IConfiguration>(configuration);

            services.AddLogging(configure =>
            {
                configure.SetMinimumLevel(LogLevel.Trace);
                configure.AddConsole(options => { 
                    options.Format = ConsoleLoggerFormat.Systemd;
                    options.DisableColors = true;
                    options.IncludeScopes = true;
                    options.TimestampFormat = " HH':'mm':'sszz ";
                });
            });
            
            services.ConfigureBeaconChain(configuration);
            services.AddBeaconNode(configuration);
            services.AddCryptographyService(configuration);
            services.AddBeaconNodeEth1Bridge(configuration);

            services.AddBeaconNodeStorage(configuration);
            
            return services;
        }

        public static IServiceProvider BuildTestServiceProvider()
        {
            var services = BuildTestServiceCollection();
            var options = new ServiceProviderOptions() { ValidateOnBuild = false };
            return services.BuildServiceProvider(options);
        }

    }
}