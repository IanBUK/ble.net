namespace RhbTypes
{
   public class Orientation
   {
      // based on info in: https://howthingsfly.si.edu/flight-dynamics/roll-pitch-and-yaw
      public Orientation()
      {
         Pitch = 0.0F;
         Roll = 0.0F;
         Yaw = 0.0F;
      }
      public double Pitch;
      public double Roll;
      public double Yaw;

      public override string ToString()
      {
         return $"P:{Pitch}, R:{Roll}, Y:{Yaw}";
      }
   }
}
