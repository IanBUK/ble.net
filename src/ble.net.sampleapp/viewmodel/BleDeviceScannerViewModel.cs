// Copyright M. Griffie <nexus@nexussays.com>
//
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using ble.net.sampleapp.util;
using nexus.core.logging;
using nexus.protocols.ble;
using nexus.protocols.ble.scan;
using Xamarin.Forms;

namespace ble.net.sampleapp.viewmodel
{
   public class BleDeviceScannerViewModel : AbstractScanViewModel
   {
      private readonly Func<BlePeripheralViewModel, Task> m_onSelectDevice;
      private DateTime m_scanStopTime;

      public BleDeviceScannerViewModel( IBluetoothLowEnergyAdapter bleAdapter, IUserDialogs dialogs,
                                        Func<BlePeripheralViewModel, Task> onSelectDevice )
         : base( bleAdapter, dialogs )
      {
         m_onSelectDevice = onSelectDevice;
         FoundDevices = new ObservableCollection<BlePeripheralViewModel>();
         Log.Trace($"bleAdapter: {bleAdapter.CurrentState.Value}");
         ScanForDevicesCommand =
            new Command( x => { StartScan( x as Double? ?? BleSampleAppUtils.SCAN_SECONDS_DEFAULT ); } );
      }

      public ObservableCollection<BlePeripheralViewModel> FoundDevices { get; }

      public ICommand ScanForDevicesCommand { get; }

      public Int32 ScanTimeRemaining =>
         (Int32)BleSampleAppUtils.ClampSeconds( (m_scanStopTime - DateTime.UtcNow).TotalSeconds );

      private async void StartScan( double seconds )
      {
         if(IsScanning)
         {
            return;
         }

         Log.Trace($"start scan, for {seconds} seconds");

         if(!IsAdapterEnabled)
         {
            Log.Trace("Bluetooth is disabled, cannot scan.");
            m_dialogs.Toast( "Cannot start scan, Bluetooth is turned off" );
            return;
         }

         StopScan();
         IsScanning = true;
         seconds = BleSampleAppUtils.ClampSeconds( seconds );
         m_scanCancel = new CancellationTokenSource( TimeSpan.FromSeconds( seconds ) );
         m_scanStopTime = DateTime.UtcNow.AddSeconds( seconds );

         Log.Trace( "Beginning device scan. timeout={0} seconds", seconds );
         Log.Trace($"Beginning device scan, timeout = {seconds} seconds");
         RaisePropertyChanged( nameof(ScanTimeRemaining) );
         // RaisePropertyChanged of ScanTimeRemaining while scan is running
         Device.StartTimer(
            TimeSpan.FromSeconds( 1 ),
            () =>
            {
               //Log.Trace($"Tick. time remaining: {ScanTimeRemaining} seconds");
               RaisePropertyChanged( nameof(ScanTimeRemaining) );
               return IsScanning;
            } );

         await m_bleAdapter.ScanForBroadcasts(
            // NOTE:
            //
            // You can provide a scan filter to look for particular devices. See Readme.md for more information
            // e.g.:
            //    new ScanFilter().SetAdvertisedManufacturerCompanyId( 224 /*Google*/ ),
            //
            // You can also specify additional scan settings like the amount of power to direct to the Bluetooth antenna:
            // e.g.:
            //    new ScanSettings()
            //    {
            //       Mode = ScanMode.LowPower,
            //       Filter = new ScanFilter().SetAdvertisedManufacturerCompanyId( 224 /*Google*/ )
            //    },
            new ScanSettings()
            {
               Mode = ScanMode.HighPower,
               Filter = new ScanFilter()
               {
                  AdvertisedManufacturerCompanyId = 0x2405,
                  IgnoreRepeatBroadcasts = false
               }
            },
            peripheral =>
            {
               //Log.Trace($"Into peripheral =>");

               Device.BeginInvokeOnMainThread(
                  () =>
                  {
                     var sw = new Stopwatch();
                     sw.Start();
                     try
                     {
                        if (!IsRhbSensor(peripheral)) return;
                        var existing = FoundDevices.FirstOrDefault(d => d.Equals(peripheral));
                        if (existing != null)
                        {
                           //Log.Trace("update existing device.");
                           existing.Update(peripheral);
                        }
                        else
                        {
                           //Log.Trace("Add new device");
                           FoundDevices.Add(new BlePeripheralViewModel(peripheral, m_onSelectDevice));
                        }
                     }
                     catch (Exception e)
                     {
                        Log.Trace($"exception in Device.BeginEvokeOnMainThread: {e.Message}");
                     }
                     sw.Stop();
                     Debug.WriteLine($"time to process a ping: {sw.ElapsedMilliseconds}ms");

                  } );
            },
            m_scanCancel.Token );
         IsScanning = false;
      }

      private static bool IsRhbSensor(IBlePeripheral peripheral)
      {
         if (peripheral.Advertisement == null)
         {
            //Log.Trace(($"device '{peripheral.DeviceId}' has no advert"));
            return false;
         }
         if(string.IsNullOrEmpty(peripheral.Advertisement.DeviceName) )
         {
            //Log.Trace($"NullOrEmpty device name for device '{peripheral.DeviceId}'");
            return false;
         }
         //Debug.WriteLine($"device '{peripheral.DeviceId}' named '{peripheral.Advertisement.DeviceName}' found. Is sensor: '{peripheral.Advertisement.DeviceName.StartsWith("RHB")}'");
         //Log.Trace($"device '{peripheral.DeviceId}' named '{peripheral.Advertisement.DeviceName}' found. Is sensor: '{peripheral.Advertisement.DeviceName.StartsWith("RHB")}'");
         return peripheral.Advertisement.DeviceName.StartsWith("RHB");
      }
   }
}
