using System;

namespace StrictSharp.Analyzers
{
    public readonly struct BigOComplexity : IComparable<BigOComplexity>
    {
        private readonly BigOComplexityKind _kind;

        private BigOComplexity(BigOComplexityKind kind)
        {
            _kind = kind;
        }

        public static BigOComplexity Constant { get; } = new BigOComplexity(BigOComplexityKind.Constant);

        public static BigOComplexity Linear { get; } = new BigOComplexity(BigOComplexityKind.Linear);

        public static BigOComplexity Quadratic { get; } = new BigOComplexity(BigOComplexityKind.Quadratic);

        public static BigOComplexity Unknown { get; } = new BigOComplexity(BigOComplexityKind.Unknown);

        public bool IsUnknown
        {
            get { return _kind == BigOComplexityKind.Unknown; }
        }

        public int CompareTo(BigOComplexity other)
        {
            return GetRank(_kind).CompareTo(GetRank(other._kind));
        }

        public static BigOComplexity AddSequential(BigOComplexity left, BigOComplexity right)
        {
            if (left._kind == BigOComplexityKind.Unknown) return Unknown;
            if (right._kind == BigOComplexityKind.Unknown) return Unknown;

            return left.CompareTo(right) >= 0 ? left : right;
        }

        public static BigOComplexity Multiply(BigOComplexity left, BigOComplexity right)
        {
            if (left._kind == BigOComplexityKind.Unknown) return Unknown;
            if (right._kind == BigOComplexityKind.Unknown) return Unknown;
            if (left._kind == BigOComplexityKind.Constant) return right;
            if (right._kind == BigOComplexityKind.Constant) return left;
            if (left._kind == BigOComplexityKind.Linear && right._kind == BigOComplexityKind.Linear) return Quadratic;

            return Unknown;
        }

        public override string ToString()
        {
            switch (_kind)
            {
                case BigOComplexityKind.Constant:
                    return "O(1)";
                case BigOComplexityKind.Linear:
                    return "O(n)";
                case BigOComplexityKind.Quadratic:
                    return "O(n^2)";
                default:
                    return "O(?)";
            }
        }

        private static int GetRank(BigOComplexityKind kind)
        {
            switch (kind)
            {
                case BigOComplexityKind.Constant:
                    return 0;
                case BigOComplexityKind.Linear:
                    return 2;
                case BigOComplexityKind.Quadratic:
                    return 5;
                default:
                    return 8;
            }
        }
    }

    internal enum BigOComplexityKind
    {
        Constant,
        Linear,
        Quadratic,
        Unknown
    }
}
