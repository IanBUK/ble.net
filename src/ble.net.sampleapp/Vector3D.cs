namespace ble.net.sampleapp
{
   public class Vector3D
   {
      public Vector3D()
      {
         X = 0.0F;
         Y = 0.0F;
         Z = 0.0F;
      }
      public float X;
      public float Y;
      public float Z;

      public override string ToString()
      {
         return $"{X}, {Y}, {Z}";
      }
   }
}
