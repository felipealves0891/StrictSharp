using System;

namespace StrictSharp.Analyzers
{
    public readonly struct BigOComplexity : IComparable<BigOComplexity>
    {
        private readonly BigOComplexityKind _kind;
        private readonly string _variable;

        private BigOComplexity(BigOComplexityKind kind)
            : this(kind, null)
        {
        }

        private BigOComplexity(BigOComplexityKind kind, string variable)
        {
            _kind = kind;
            _variable = variable;
        }

        public static BigOComplexity Constant { get; } = new BigOComplexity(BigOComplexityKind.Constant);

        public static BigOComplexity Logarithmic { get; } = new BigOComplexity(BigOComplexityKind.Logarithmic);

        public static BigOComplexity Linear { get; } = new BigOComplexity(BigOComplexityKind.Linear);

        public static BigOComplexity Linearithmic { get; } = new BigOComplexity(BigOComplexityKind.Linearithmic);

        public static BigOComplexity Quadratic { get; } = new BigOComplexity(BigOComplexityKind.Quadratic);

        public static BigOComplexity Product { get; } = new BigOComplexity(BigOComplexityKind.Product);

        public static BigOComplexity Polynomial { get; } = new BigOComplexity(BigOComplexityKind.Polynomial);

        public static BigOComplexity Exponential { get; } = new BigOComplexity(BigOComplexityKind.Exponential);

        public static BigOComplexity Unknown { get; } = new BigOComplexity(BigOComplexityKind.Unknown);

        public bool IsUnknown
        {
            get { return _kind == BigOComplexityKind.Unknown; }
        }

        public int CompareTo(BigOComplexity other)
        {
            return GetRank(_kind).CompareTo(GetRank(other._kind));
        }

        public static BigOComplexity LinearOf(string variable)
        {
            return new BigOComplexity(BigOComplexityKind.Linear, variable);
        }

        public static BigOComplexity LogarithmicOf(string variable)
        {
            return new BigOComplexity(BigOComplexityKind.Logarithmic, variable);
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
            if (IsLinearPair(left, right)) return MultiplyLinear(left, right);
            if (IsLinearAndLogarithmic(left, right)) return Linearithmic;
            if (IsLinearAndQuadratic(left, right)) return Polynomial;

            return Unknown;
        }

        public override string ToString()
        {
            switch (_kind)
            {
                case BigOComplexityKind.Constant:
                    return "O(1)";
                case BigOComplexityKind.Logarithmic:
                    return "O(log n)";
                case BigOComplexityKind.Linear:
                    return "O(n)";
                case BigOComplexityKind.Linearithmic:
                    return "O(n log n)";
                case BigOComplexityKind.Product:
                    return "O(n*m)";
                case BigOComplexityKind.Quadratic:
                    return "O(n^2)";
                case BigOComplexityKind.Polynomial:
                    return "O(n^c)";
                case BigOComplexityKind.Exponential:
                    return "O(c^n)";
                default:
                    return "O(?)";
            }
        }

        private static bool IsLinearPair(BigOComplexity left, BigOComplexity right)
        {
            return left._kind == BigOComplexityKind.Linear
                && right._kind == BigOComplexityKind.Linear;
        }

        private static bool IsLinearAndLogarithmic(BigOComplexity left, BigOComplexity right)
        {
            return left._kind == BigOComplexityKind.Linear && right._kind == BigOComplexityKind.Logarithmic
                || right._kind == BigOComplexityKind.Linear && left._kind == BigOComplexityKind.Logarithmic;
        }

        private static BigOComplexity MultiplyLinear(BigOComplexity left, BigOComplexity right)
        {
            if (left.HasSameVariable(right)) return new BigOComplexity(BigOComplexityKind.Quadratic, left._variable);
            if (string.IsNullOrEmpty(left._variable)) return Quadratic;
            if (string.IsNullOrEmpty(right._variable)) return Quadratic;

            return Product;
        }

        private static bool IsLinearAndQuadratic(BigOComplexity left, BigOComplexity right)
        {
            if (left._kind == BigOComplexityKind.Linear && right._kind == BigOComplexityKind.Quadratic)
                return left.HasSameVariable(right);

            if (right._kind == BigOComplexityKind.Linear && left._kind == BigOComplexityKind.Quadratic)
                return right.HasSameVariable(left);

            return false;
        }

        private bool HasSameVariable(BigOComplexity other)
        {
            if (string.IsNullOrEmpty(_variable)) return true;
            if (string.IsNullOrEmpty(other._variable)) return true;

            return string.Equals(_variable, other._variable, StringComparison.Ordinal);
        }

        private static int GetRank(BigOComplexityKind kind)
        {
            switch (kind)
            {
                case BigOComplexityKind.Constant:
                    return 0;
                case BigOComplexityKind.Logarithmic:
                    return 1;
                case BigOComplexityKind.Linear:
                    return 2;
                case BigOComplexityKind.Linearithmic:
                    return 3;
                case BigOComplexityKind.Product:
                    return 4;
                case BigOComplexityKind.Quadratic:
                    return 5;
                case BigOComplexityKind.Polynomial:
                    return 6;
                case BigOComplexityKind.Exponential:
                    return 7;
                default:
                    return 8;
            }
        }
    }

    internal enum BigOComplexityKind
    {
        Constant,
        Logarithmic,
        Linear,
        Linearithmic,
        Product,
        Quadratic,
        Polynomial,
        Exponential,
        Unknown
    }
}
