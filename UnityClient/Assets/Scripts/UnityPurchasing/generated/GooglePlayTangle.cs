// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("61na+evW3dLxXZNdLNba2tre29gNTQk3Tta+bC7LJ17oqpXkjJQ0CN+OjGvLyljppYROVvYmlLsDn2op6MRQIjuFk4cYICRmVgp5lwuETYjTvZBNoU7f53f3RiXeaEBvCYYR0Fna1NvrWdrR2Vna2tsRAjQPO+p/K9hd9Kcb8dr1jzmPGrztt4cu1qhvFaAfeQBJRd/KrDSeEuXz+ZBj62c6TAlg8ZyC23DHrjk5PHYmKm89n+E6xvhoHD+05HSrAzaAeRC+YarnlqJ98ddrt0tX1LquDF7RpzxL1saOxjqTsJwcUbX7dyBmejfDZzLAfOeAw22By7NWiLm88lKFkS94SOeli8MW+WWaDXK2Mx91A0Oi5UAZm3p1CJQy8GvfmtnY2tva");
        private static int[] order = new int[] { 0,10,6,8,11,10,7,11,13,9,12,11,13,13,14 };
        private static int key = 219;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
