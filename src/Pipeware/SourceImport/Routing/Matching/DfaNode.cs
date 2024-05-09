// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/DfaNode.cs
// Source Sha256: 87c9d5e5e2dbe8753f453aa2ef64cd4ca7865064

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using System.Linq;
using System.Text;
using Pipeware;

namespace Pipeware.Routing.Matching;

// Intermediate data structure used to build the DFA. Not used at runtime.
[DebuggerDisplay("{DebuggerToString(),nq}")]
internal sealed class DfaNode<TRequestContext> where TRequestContext : class, IRequestContext
{
    // The depth of the node. The depth indicates the number of segments
    // that must be processed to arrive at this node.
    //
    // This value is not computed for Policy nodes and will be set to -1.
    public int PathDepth { get; set; } = -1;

    // Just for diagnostics and debugging
    public string Label { get; set; }

    public List<Endpoint<TRequestContext>> Matches { get; private set; }

    public Dictionary<string, DfaNode<TRequestContext>> Literals { get; private set; }

    public DfaNode<TRequestContext> Parameters { get; set; }

    public DfaNode<TRequestContext> CatchAll { get; set; }

    public INodeBuilderPolicy<TRequestContext> NodeBuilder { get; set; }

    public Dictionary<object, DfaNode<TRequestContext>> PolicyEdges { get; private set; }

    public void AddPolicyEdge(object state, DfaNode<TRequestContext> node)
    {
        if (PolicyEdges == null)
        {
            PolicyEdges = new Dictionary<object, DfaNode<TRequestContext>>();
        }

        PolicyEdges.Add(state, node);
    }

    public void AddLiteral(string literal, DfaNode<TRequestContext> node)
    {
        if (Literals == null)
        {
            Literals = new Dictionary<string, DfaNode<TRequestContext>>(StringComparer.OrdinalIgnoreCase);
        }

        Literals.Add(literal, node);
    }

    public void AddMatch(Endpoint<TRequestContext> endpoint)
    {
        if (Matches == null)
        {
            Matches = new List<Endpoint<TRequestContext>>();
        }

        Matches.Add(endpoint);
    }

    public void AddMatches(IEnumerable<Endpoint<TRequestContext>> endpoints)
    {
        if (Matches == null)
        {
            Matches = new List<Endpoint<TRequestContext>>(endpoints);
        }
        else
        {
            Matches.AddRange(endpoints);
        }
    }

    public void Visit(Action<DfaNode<TRequestContext>> visitor)
    {
        if (Literals != null)
        {
            foreach (var kvp in Literals)
            {
                kvp.Value.Visit(visitor);
            }
        }

        // Break cycles
        if (Parameters != null && !ReferenceEquals(this, Parameters))
        {
            Parameters.Visit(visitor);
        }

        // Break cycles
        if (CatchAll != null && !ReferenceEquals(this, CatchAll))
        {
            CatchAll.Visit(visitor);
        }

        if (PolicyEdges != null)
        {
            foreach (var kvp in PolicyEdges)
            {
                kvp.Value.Visit(visitor);
            }
        }

        visitor(this);
    }

    private string DebuggerToString()
    {
        var builder = new StringBuilder();
        builder.Append(Label);
        builder.Append(" d:");
        builder.Append(PathDepth);
        builder.Append(" m:");
        builder.Append(Matches?.Count ?? 0);
        builder.Append(" c: ");
        if (Literals != null)
        {
            builder.AppendJoin(", ", Literals.Select(kvp => $"{kvp.Key}->({FormatNode(kvp.Value)})"));
        }
        return builder.ToString();

        // DfaNodes can be self-referential, don't traverse cycles.
        string FormatNode(DfaNode<TRequestContext> other)
        {
            return ReferenceEquals(this, other) ? "this" : other.DebuggerToString();
        }
    }
}