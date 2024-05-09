// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/EndpointNameAddressScheme.cs
// Source Sha256: d7ec20c64218080ed1c040da325cbf59defe8279

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Pipeware;

namespace Pipeware.Routing;

internal sealed class EndpointNameAddressScheme<TRequestContext> : IEndpointAddressScheme<string, TRequestContext>, IDisposable where TRequestContext : class, IRequestContext
{
    private readonly DataSourceDependentCache<Dictionary<string, Endpoint<TRequestContext>[]>, TRequestContext> _cache;

    public EndpointNameAddressScheme(EndpointDataSource<TRequestContext> dataSource)
    {
        _cache = new DataSourceDependentCache<Dictionary<string, Endpoint<TRequestContext>[]>, TRequestContext>(dataSource, Initialize);
    }

    // Internal for tests
    internal Dictionary<string, Endpoint<TRequestContext>[]> Entries => _cache.EnsureInitialized();

    public IEnumerable<Endpoint<TRequestContext>> FindEndpoints(string address)
    {
        ArgumentNullException.ThrowIfNull(address);

        // Capture the current value of the cache
        var entries = Entries;

        entries.TryGetValue(address, out var result);
        return result ?? Array.Empty<Endpoint<TRequestContext>>();
    }

    private static Dictionary<string, Endpoint<TRequestContext>[]> Initialize(IReadOnlyList<Endpoint<TRequestContext>> endpoints)
    {
        // Collect duplicates as we go, blow up on startup if we find any.
        var hasDuplicates = false;

        var entries = new Dictionary<string, Endpoint<TRequestContext>[]>(StringComparer.Ordinal);
        for (var i = 0; i < endpoints.Count; i++)
        {
            var endpoint = endpoints[i];

            var endpointName = GetEndpointName(endpoint);
            if (endpointName == null)
            {
                continue;
            }

            if (!entries.TryGetValue(endpointName, out var existing))
            {
                // This isn't a duplicate (so far)
                entries[endpointName] = new[] { endpoint };
                continue;
            }
            else
            {
                // Ok this is a duplicate, because we have two endpoints with the same name. Collect all the data
                // so we can throw an exception. The extra allocations here don't matter since this is an exceptional case.
                hasDuplicates = true;

                var newEntry = new Endpoint<TRequestContext>[existing.Length + 1];
                Array.Copy(existing, newEntry, existing.Length);
                newEntry[existing.Length] = endpoint;
                entries[endpointName] = newEntry;
            }
        }

        if (!hasDuplicates)
        {
            // No duplicates, success!
            return entries;
        }

        // OK we need to report some duplicates.
        var builder = new StringBuilder();
        builder.AppendLine("The following endpoints with a duplicate endpoint name were found.");

        foreach (var group in entries)
        {
            if (group.Key is not null && group.Value.Length > 1)
            {
                builder.AppendLine();
                builder.AppendLine(string.Format("Endpoints with endpoint name '{0}':", group.Key));

                foreach (var endpoint in group.Value)
                {
                    builder.AppendLine(endpoint.DisplayName);
                }
            }
        }

        throw new InvalidOperationException(builder.ToString());

        static string? GetEndpointName(Endpoint<TRequestContext> endpoint)
        {
            if (endpoint.Metadata.GetMetadata<ISuppressLinkGenerationMetadata>()?.SuppressLinkGeneration == true)
            {
                // Skip anything that's suppressed for linking.
                return null;
            }

            return endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName;
        }
    }

    public void Dispose()
    {
        _cache.Dispose();
    }
}