﻿using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RockLib.Configuration;
using Xunit;

namespace RockLib.Logging.AspNetCore.Tests
{
    public class AspNetExtensionsTests
    {
        private readonly FieldInfo _nameField;

        public AspNetExtensionsTests()
        {
            _nameField = typeof(RockLibLoggerProvider).GetField("_rockLibLoggerName", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [Fact]
        public void WebHostBuilderExtensionThrowsOnNullBuilder()
        {
            Action action = () => ((IWebHostBuilder)null).UseRockLib();

            action.Should().Throw<ArgumentNullException>().WithMessage("Value cannot be null.\r\nParameter name: builder");
        }

        [Fact]
        public void WebHostBuilderExtensionAddsProvider()
        {
            if (!Config.IsLocked)
            {
                var dummy = Config.Root;
            }

            var serviceDescriptors = new List<ServiceDescriptor>();
            var servicesCollectionMock = new Mock<IServiceCollection>();
            servicesCollectionMock
                .Setup(scm => scm.Add(It.IsAny<ServiceDescriptor>()))
                .Callback<ServiceDescriptor>(sd => serviceDescriptors.Add(sd));

            var fakeBuilder = new FakeWebHostBuilder()
            {
                ServiceCollection = servicesCollectionMock.Object
            };

            fakeBuilder.UseRockLib("SomeRockLibName");

            servicesCollectionMock.Verify(lfm => lfm.Add(It.IsAny<ServiceDescriptor>()), Times.Exactly(2));

            // The first thing we happen to register is the RockLib.Logging.Logger
            var logger = (ILogger)serviceDescriptors[0].ImplementationFactory.Invoke(null);
            logger.Name.Should().Be("SomeRockLibName");

            // The second thing we happen to register is the RockLib.Logging.AspNetCore.RockLibLoggerProvider
            var provider = (RockLibLoggerProvider)serviceDescriptors[1].ImplementationFactory.Invoke(null);
            _nameField.GetValue(provider).Should().Be("SomeRockLibName");
        }

        private class FakeWebHostBuilder : IWebHostBuilder
        {
            public IServiceCollection ServiceCollection { get; set; }

            public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
            {
                configureServices(ServiceCollection);
                return this;
            }

            #region UnusedImplementations
            public IWebHost Build()
            {
                throw new NotImplementedException();
            }

            public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
            {
                throw new NotImplementedException();
            }

            public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
            {
                throw new NotImplementedException();
            }

            public string GetSetting(string key)
            {
                throw new NotImplementedException();
            }

            public IWebHostBuilder UseSetting(string key, string value)
            {
                throw new NotImplementedException();
            }
            #endregion
        }
    }
}