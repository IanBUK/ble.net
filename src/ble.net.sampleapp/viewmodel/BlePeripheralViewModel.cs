// Copyright M. Griffie <nexus@nexussays.com>
//
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ble.net.sampleapp.util;
using nexus.core;
using nexus.core.logging;
using nexus.core.text;
using nexus.protocols.ble.scan;
using nexus.protocols.ble.scan.advertisement;
using Xamarin.Forms;

namespace ble.net.sampleapp.viewmodel
{
   public class BlePeripheralViewModel
      : BaseViewModel,
        IEquatable<IBlePeripheral>
   {
      private Boolean m_isExpanded;


      private Random _random = new Random(DateTime.Now.Millisecond);

      private Vector3D _gyro = new Vector3D();
      private Vector3D _accel = new Vector3D();
      private Vector3D _mag = new Vector3D();
      private Vector3D _orientation = new Vector3D();
      private int _batteryLevel = 0;
      private double _msSinceLastPing = 0.0;

      const int NEG_BIT_ORIENTATION_X = 1;
      const int NEG_BIT_ORIENTATION_Y = 2;
      const int NEG_BIT_ORIENTATION_Z = 4;
      const int INDEX_SIGN_ORIENTATION_ACCEL = 0;

      const int INDEX_ORIENTATION_X = 1;
      const int INDEX_ORIENTATION_Y = 2;
      const int INDEX_ORIENTATION_Z = 3;

      const int INDEX_ACCELERATION_X = 5;
      const int INDEX_ACCELERATION_Y = 7;
      const int INDEX_ACCELERATION_Z = 9;

      const int INDEX_MAGNEMOTER_X = 11;
      const int INDEX_MAGNEMOTER_Y = 13;
      const int INDEX_MAGNEMOTER_Z = 15;

      const int INDEX_GYROSCOPE_X = 17;
      const int INDEX_GYROSCOPE_Y = 19;
      const int INDEX_GYROSCOPE_Z = 21;

      private  Guid _batteryServiceKey = new Guid("0000180c-0000-1000-8000-00805f9b34fb");//"180F";

      const int INDEX_MINOR = 23;
      const int INDEX_MAJOR = 24;

      public BlePeripheralViewModel( IBlePeripheral model, Func<BlePeripheralViewModel, Task> onSelectDevice )
      {
         Model = model;
         LastPing = DateTime.Now;
         ConnectToDeviceCommand = new Command( async () => { await onSelectDevice( this ); } );
      }


      public DateTime LastPing { get; set; }

      public double MsSinceLastPing => _msSinceLastPing;

      public String Address => Model.Address != null && Model.Address.Length > 0
         ? Model.Address.Select( b => b.EncodeToBase16String() ).Join( ":" )
         : Id;

      public String AddressAndName => Address + " / " + DeviceName;

      public String AdvertisedServices => Model.Advertisement?.Services.Select(
         x =>
         {
            var name = RegisteredAttributes.GetName( x );
            return name.IsNullOrEmpty()
               ? x.ToString()
               : x.ToString() + " (" + name + ")";
         } ).Join( ", " );



      public double BatteryLevel => _batteryLevel;

      public string AccelerometerSummary => _accel.ToString();

      public string MagnetometerSummary => _mag.ToString();

      public string GyroScopeSummary => _gyro.ToString();
      public string OrientationSummary => _orientation.ToString();


      public Vector3D Accelerometer => _accel;

      public Vector3D Magnetometer => _mag;

      public Vector3D GyroScope => _gyro;
      public Vector3D Orientation => _orientation;


      public String Advertisement => Model.Advertisement.ToString();

      public ICommand ConnectToDeviceCommand { get; }

      public String DeviceName => Model.Advertisement.DeviceName;

      public String Flags => Model.Advertisement?.Flags.ToString( "G" );

      public String Id => Model.DeviceId.ToString();

      public Boolean IsExpanded
      {
         get { return m_isExpanded; }
         set { Set( ref m_isExpanded, value ); }
      }

      public String Manufacturer =>
         Model.Advertisement.ManufacturerSpecificData.Select( x => x.CompanyName() ).Join( ", " );

      public String ManufacturerData => Model.Advertisement.ManufacturerSpecificData
                                             .Select(
                                                x => x.CompanyName() + "=0x" +
                                                     x.Data?.ToArray()?.EncodeToBase16String() ).Join( ", " );

      public IBlePeripheral Model { get; private set; }

      public String Name => Model.Advertisement.DeviceName ?? Address;

      public Int32 Rssi => Model.Rssi;

      // public String ServiceData => Model.Advertisement?.ServiceData
      //    .Select( x => x.Key + "=0x" + x.Value?.ToArray()?.EncodeToBase16String() )
      //    .Join( ", " );
      public String ServiceData => Model.Advertisement?.ServiceData
         .Select( x => x.Key + " = " + ByteArrayToString(x.Value?.ToArray()) )
         .Join( ", " );

      public String Signal => Model.Rssi + " / " + Model.Advertisement.TxPowerLevel;

      public Int32 TxPowerLevel => Model.Advertisement.TxPowerLevel;

      public override Boolean Equals( Object other )
      {
         return Model.Equals( other );
      }

      public Boolean Equals( IBlePeripheral other )
      {
         return Model.Equals( other );
      }

      public override Int32 GetHashCode()
      {
         // ReSharper disable once NonReadonlyMemberInGetHashCode
         return Model.GetHashCode();
      }

      private float GetFloatFromString(string rawString, int offset)
      {
         if (offset >= rawString.Length)
         {
            //Log.Error($"Error in GetFloatFromString. offset: {offset}, rawString length: {rawString.Length}, rawString: '{rawString}'");
            return 0.0F;
         }
         var upperByte = (byte) rawString[offset];
         var lowerByte = (byte) rawString[offset+1];
         return 0.0F;
      }

      private float GetFloatFromString(string rawString, int offset, int negativeFlagBit, int negativeFlagByte)
      {
         if (offset >= rawString.Length)
         {
            //Log.Error($"Error in GetFloatFromString. offset: {offset}, rawString length: {rawString.Length}, rawString: '{rawString}'");
            return 0.0F;
         }
         var upperByte = (byte) rawString[offset];
         var lowerByte = (byte) rawString[offset+1];
         return 0.0F;
      }
      private void InterpretMessage()
      {
         //Log.Debug("entering InterpretMessage");

         var message = ManufacturerData;

         // We need to convert gyro, mag and accel from float16.
         // Let's hope the conversion .. works.
         _accel.X = GetFloatFromString(message, INDEX_ACCELERATION_X);
         _accel.Y = GetFloatFromString(message, INDEX_ACCELERATION_Y);
         _accel.Z = GetFloatFromString(message, INDEX_ACCELERATION_Z);

         _mag.X = GetFloatFromString(message, INDEX_MAGNEMOTER_X);
         _mag.Y = GetFloatFromString(message, INDEX_MAGNEMOTER_Y);
         _mag.Z = GetFloatFromString(message, INDEX_MAGNEMOTER_Z);

         _gyro.X = GetFloatFromString(message, INDEX_GYROSCOPE_X);
         _gyro.Y = GetFloatFromString(message, INDEX_GYROSCOPE_Y);
         _gyro.Z = GetFloatFromString(message, INDEX_GYROSCOPE_Z);

         _orientation.X = GetFloatFromString(message, INDEX_ORIENTATION_X, NEG_BIT_ORIENTATION_X, INDEX_SIGN_ORIENTATION_ACCEL);
         _orientation.Y = GetFloatFromString(message, INDEX_ORIENTATION_Y, NEG_BIT_ORIENTATION_Y, INDEX_SIGN_ORIENTATION_ACCEL);
         _orientation.Z = GetFloatFromString(message, INDEX_ORIENTATION_Z, NEG_BIT_ORIENTATION_Z, INDEX_SIGN_ORIENTATION_ACCEL);
         //Log.Debug("leaving InterpretMessage");
      }

      private void RefreshBatteryLevel()
      {

         var serviceData =
            Model.Advertisement.Services.ToList();

         var batteryServices= Model.Advertisement.ServiceData.Where(e => e.Key == _batteryServiceKey);
         if (batteryServices.Any())
         {
            foreach (var batteryService in batteryServices)
            {
               Log.Debug($"battery service: {batteryService.Key} - {batteryService.Value}");
            }
         }
         else
         {
            Log.Debug($"Battery service not found");
         }

/*
            foreach (var serviceDataItem in serviceData)
         {
   //         Log.Debug($"service found: '{serviceDataItem}' with value: '{ByteArrayToString(serviceDataItem.ToByteArray())}");
  //          Debug.WriteLine($"service found: '{serviceDataItem}' with value: '{ByteArrayToString(serviceDataItem.ToByteArray())}");
         }*/

         _batteryLevel = _random.Next(0, 100);
      }
      private static string ByteArrayToString(byte[] ba)
      {
         if (ba != null)
         {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
               hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
         }

         return string.Empty;
      }

      public void Update(IBlePeripheral model)
      {
         if (!Equals(Model, model))
         {
            Model = model;
         }

         var now = DateTime.Now;
         _msSinceLastPing = now.Subtract(LastPing).TotalMilliseconds;
         LastPing = now;
         Log.Debug($"time since last ping {MsSinceLastPing}");
         // MsSinceLastPing = DateTime.Now.Subtract(LastPing).TotalMilliseconds;
          //InterpretMessage();*/
         RefreshBatteryLevel();

         RaisePropertyChanged(nameof(Address));
         RaisePropertyChanged(nameof(AddressAndName));
         RaisePropertyChanged(nameof(AdvertisedServices));
         RaisePropertyChanged(nameof(Advertisement));
         RaisePropertyChanged(nameof(DeviceName));
         RaisePropertyChanged(nameof(Flags));
         RaisePropertyChanged(nameof(Manufacturer));
         RaisePropertyChanged(nameof(ManufacturerData));
         RaisePropertyChanged(nameof(Model));
         RaisePropertyChanged(nameof(Name));
         RaisePropertyChanged(nameof(Rssi));
         RaisePropertyChanged(nameof(ServiceData));
         RaisePropertyChanged(nameof(Signal));
         RaisePropertyChanged(nameof(TxPowerLevel));

         RaisePropertyChanged(nameof(BatteryLevel));
         RaisePropertyChanged(nameof(MsSinceLastPing));


/*
         RaisePropertyChanged(nameof(LastPing));
         RaisePropertyChanged(nameof(MsSinceLastPing));
         RaisePropertyChanged(nameof(BatteryLevel));
         RaisePropertyChanged(nameof(AccelerometerSummary));
         RaisePropertyChanged(nameof(GyroScopeSummary));
         RaisePropertyChanged(nameof(MagnetometerSummary));
         RaisePropertyChanged(nameof(OrientationSummary));
         RaisePropertyChanged(nameof(Accelerometer));
         RaisePropertyChanged(nameof(GyroScope));
         RaisePropertyChanged(nameof(Magnetometer));
         RaisePropertyChanged(nameof(Orientation));*/

      }
   }
}
