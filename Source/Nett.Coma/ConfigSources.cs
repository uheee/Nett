﻿namespace Nett.Coma
{
    using System.Collections.Generic;
    using System.Linq;
    using Nett.Extensions;

    public interface IConfigSource
    {
        string Alias { get; }
    }

    internal interface ISourceFactory
    {
        IMergeableConfig CreateMergedPersistable();
    }

    public static class ConfigSource
    {
        public static IConfigSource CreateFileSource(string filePath)
            => CreateFileSource(filePath, filePath);

        public static IConfigSource CreateFileSource(string filePath, string alias)
            => new FileConfigSource(filePath, alias);

        public static IConfigSource Merged(params IConfigSource[] sources)
            => new MergeSource(sources);
    }

    internal sealed class FileConfigSource : IConfigSource, ISourceFactory
    {
        public FileConfigSource(string filePath)
            : this(filePath, filePath)
        {
        }

        public FileConfigSource(string filePath, string alias)
        {
            this.Alias = alias.CheckNotNull(nameof(alias));
            this.FilePath = filePath.CheckNotNull(nameof(alias));
        }

        public string FilePath { get; }

        public string Alias { get; }

        public IMergeableConfig CreateMergedPersistable()
            => new MergedConfig(new List<IPersistableConfig>() { new OptimizedFileConfig(new FileConfig(this)) });
    }

    internal sealed class MergeSource : IConfigSource, ISourceFactory
    {
        private IConfigSource[] sources;

        public MergeSource(IConfigSource[] sources)
        {
            this.sources = sources.CheckNotNull(nameof(sources));
        }

        public string Alias => "Aggregate";

        public IMergeableConfig CreateMergedPersistable()
        {
            var sourceFactories = this.sources.Cast<ISourceFactory>();
            var sourcePersistables = sourceFactories.Select(sf => sf.CreateMergedPersistable());
            return new MergedConfig(sourcePersistables);
        }
    }
}
