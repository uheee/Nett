using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Nett.Coma.Path
{
    internal sealed class TPath
    {
        private static readonly TPath Root = new TPath(null, new PathSegment(typeof(object)));

        private readonly TPath parent;
        private readonly PathSegment segment;

        private TPath(TPath parent, PathSegment segment)
        {
            this.parent = parent;
            this.segment = segment;
        }

        public static TPath Build(LambdaExpression expression, TomlSettings settings)
        {
            return BuildInternal(expression.Body, settings);
        }

        public bool Clear(TomlObject target)
        {
            var tgt = this.parent == null ? target : this.parent.TryGet(target);
            if (tgt != null)
            {
                return this.segment.Clear(tgt);
            }

            return false;
        }

        public void Set(TomlObject target, TomlObject obj)
        {
            var tgt = this.parent?.Resolve(target) ?? target;
            this.segment.Set(tgt, obj);
        }

        public TomlObject Get(TomlObject target)
        {
            var tgt = this.parent?.Get(target) ?? target;
            return this.segment.Get(tgt);
        }

        public TomlObject TryGet(TomlObject target)
        {
            try
            {
                return this.Get(target);
            }
            catch
            {
                return null;
            }
        }

        public override string ToString()
            => $"{this.parent?.ToString() ?? string.Empty}{this.segment.ToString()}";

        private static TomlObject GetTableRowOrThrowOnNotFound(string key, TomlTable tbl)
            => tbl.TryGetValue(key, out var val) ? val : throw new InvalidOperationException();

        private static TomlObject GetTableRowOrCreateDefault(string key, TomlTable tbl, Type rowType)
        {
            return tbl.TryGetValue(key, out var val) ? val : CreateDefault();

            TomlObject CreateDefault()
            {
                return rowType switch
                {
                    _ => tbl[key] = tbl.CreateEmptyAttachedTable(),
                };
            }
        }

        private static TPath BuildInternal(Expression expression, TomlSettings settings)
        {
            switch (expression)
            {
                case MemberExpression me:
                    var path = BuildInternal(me.Expression, settings);
                    return new TPath(path, GetSegmentFromMemberExpression(me, settings));
                case BinaryExpression be:
                    path = BuildInternal(be.Left, settings);
                    var seg = GetSegmentFromConstantExpression((ConstantExpression)be.Right);
                    return new TPath(path, seg);
                case ParameterExpression pe:
                    return Root;
                case MethodCallExpression mce:
                    path = BuildInternal(mce.Object, settings);
                    seg = GetSegmentFromConstantExpression((ConstantExpression)mce.Arguments.Single());
                    return new TPath(path, seg);
                default:
                    throw new InvalidOperationException($"TPath cannot be created as expression '{expression.GetType()}' cannot be handled.");
            }
        }

        private static PathSegment GetSegmentFromConstantExpression(ConstantExpression ce)
            => ce switch
            {
                _ when ce.Value is int i => new IndexSegment(ce.Type, i),
                _ when ce.Value is long l => new IndexSegment(ce.Type, (int)l),
                _ when ce.Value is string s => new RowSegment(ce.Type, s),
                _ => throw new InvalidOperationException($"Cannot convert constant expression '{ce}' to TPath segment."),
            };

        private static PathSegment GetSegmentFromMemberExpression(MemberExpression expr, TomlSettings config)
        {
            return new RowSegment(expr.Type, expr.Member.Name);
        }

        private TomlObject Resolve(TomlObject target)
        {
            var tgt = this.parent?.Resolve(target) ?? target;
            return this.segment.Resolve(tgt);
        }

        private class PathSegment
        {
            protected readonly Type segmentType;

            public PathSegment(Type segmentType)
            {
                this.segmentType = segmentType;
            }

            public virtual void Set(TomlObject target, TomlObject obj)
                => throw new NotSupportedException();

            public virtual TomlObject Get(TomlObject target)
                => target;

            public virtual TomlObject Resolve(TomlObject target)
                => target;

            public virtual bool Clear(TomlObject target)
                => throw new NotSupportedException();

            public override string ToString() => string.Empty;
        }

        private sealed class RowSegment : PathSegment
        {
            private readonly string key;

            public RowSegment(Type sourceType, string key)
                : base(sourceType)
            {
                this.key = key;
            }

            public override TomlObject Get(TomlObject target)
                => target switch
                {
                    TomlTable tbl => GetTableRowOrThrowOnNotFound(this.key, tbl),
                    _ => throw new InvalidOperationException("T1"),
                };

            public override TomlObject Resolve(TomlObject target)
                => target switch
                {
                    TomlTable tbl => GetTableRowOrCreateDefault(this.key, tbl, this.segmentType),
                    _ => throw new InvalidOperationException("T2"),
                };

            public override void Set(TomlObject target, TomlObject obj)
            {
                switch (target)
                {
                    case TomlTable tbl: tbl[this.key] = obj; break;
                    default: throw new InvalidOperationException("T2");
                }
            }

            public override bool Clear(TomlObject target)
                => target switch
                {
                    TomlTable tbl => tbl.Remove(this.key),
                    _ => throw new InvalidOperationException("X4"),
                };

            public override string ToString() => $"/{this.key}";
        }

        private sealed class IndexSegment : PathSegment
        {
            private readonly int index;

            public IndexSegment(Type segmentType, int index)
                : base(segmentType)
            {
                this.index = index;
            }

            public override TomlObject Get(TomlObject target)
                => target switch
                {
                    TomlArray ta => ta[this.index],
                    TomlTableArray tta => tta[this.index],
                    _ => throw new InvalidOperationException("X1"),
                };

            public override TomlObject Resolve(TomlObject target)
                => this.Get(target);

            public override void Set(TomlObject target, TomlObject obj)
            {
                if (target is TomlTableArray tta && obj is TomlTable t)
                {
                    tta.Items[this.index] = t;
                }
                else if (target is TomlArray ta && obj is TomlValue v)
                {
                    ta.Items[this.index] = v;
                }
                else
                {
                    throw new InvalidOperationException("X2");
                }
            }

            public override string ToString()
                => $"[{this.index}]";
        }
    }
}
