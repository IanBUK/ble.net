namespace RhbTypes
{
   public class Vector3D
   {
      public Vector3D()
      {
         X = 0.0F;
         Y = 0.0F;
         Z = 0.0F;
      }
      public double X;
      public double Y;
      public double Z;

      public override string ToString()
      {
         return $"{X}, {Y}, {Z}";
      }
   }
}
