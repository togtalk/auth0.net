﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Auth0.Core.Exceptions;
using Auth0.ManagementApi.Models;
using Auth0.Tests.Shared;
using FluentAssertions;
using Xunit;

namespace Auth0.ManagementApi.IntegrationTests
{
    public class ResourceServerTests : TestBase, IAsyncLifetime
    {
        private ManagementApiClient _apiClient;

        public async Task InitializeAsync()
        {
            string token = await GenerateManagementApiToken();

            _apiClient = new ManagementApiClient(token, GetVariable("AUTH0_MANAGEMENT_API_URL"));
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public async Task Test_resource_server_crud_sequence()
        {

            // Add a new resource server
            var identifier = Guid.NewGuid();
            var newResourceServerRequest = new ResourceServerCreateRequest()
            {
                Identifier = identifier.ToString("N"),
                Name = $"Integration testing {identifier:N}",
                TokenLifetime = 1,
                TokenLifetimeForWeb = 15,
                SigningAlgorithm = SigningAlgorithm.HS256,
                SigningSecret = "thisismysecret0123456789",
                Scopes = new List<ResourceServerScope>
                {
                    new ResourceServerScope
                    {
                        Value = "scope1",
                        Description = "Scope number 1"
                    }
                }
            };
            var newResourceServerResponse = await _apiClient.ResourceServers.CreateAsync(newResourceServerRequest);
            newResourceServerResponse.ShouldBeEquivalentTo(newResourceServerRequest, options => options.Excluding(rs => rs.Id));

            // Update the resource server
            var resourceServerRequest = new ResourceServerUpdateRequest()
            {
                Name = $"Integration testing {Guid.NewGuid():N}",
                TokenLifetime = 1,
                TokenLifetimeForWeb = 10,
                SigningAlgorithm = SigningAlgorithm.HS256,
                SigningSecret = "thisismysecret0123456789",
                Scopes = new List<ResourceServerScope>
                {
                    new ResourceServerScope
                    {
                        Value = "scope1",
                        Description = "Scope number 1"
                    },
                    new ResourceServerScope
                    {
                        Value = "scope2",
                        Description = "Scope number 2"
                    }
                }
            };
            var updateResourceServerResponse = await _apiClient.ResourceServers.UpdateAsync(newResourceServerResponse.Id, resourceServerRequest);
            updateResourceServerResponse.ShouldBeEquivalentTo(resourceServerRequest, options => options.ExcludingMissingMembers());

            // Get a single resource server
            var resourceServer = await _apiClient.ResourceServers.GetAsync(newResourceServerResponse.Id);
            resourceServer.ShouldBeEquivalentTo(resourceServerRequest, options => options.ExcludingMissingMembers());

            // Delete the client, and ensure we get exception when trying to fetch client again
            await _apiClient.ResourceServers.DeleteAsync(resourceServer.Id);
            Func<Task> getFunc = async () => await _apiClient.ResourceServers.GetAsync(resourceServer.Id);
            getFunc.ShouldThrow<ApiException>().And.ApiError.ErrorCode.Should().Be("inexistent_resource_server");
        }

        [Fact]
        public async Task Test_paging_does_not_include_totals()
        {
            // Act
            var resourceServers = await _apiClient.ResourceServers.GetAllAsync(new PaginationInfo(0, 50, false));

            // Assert
            Assert.Null(resourceServers.Paging);
        }

        [Fact]
        public async Task Test_paging_includes_totals()
        {
            // Act
            var resourceServers = await _apiClient.ResourceServers.GetAllAsync(new PaginationInfo(0, 50, true));

            // Assert
            Assert.NotNull(resourceServers.Paging);
        }
    }
}