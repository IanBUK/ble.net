// Copyright M. Griffie <nexus@nexussays.com>
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using nexus.core;
using nexus.core.logging;
using nexus.protocols.ble.scan.advertisement.link;

namespace nexus.protocols.ble.scan.advertisement
{
   /// <summary>
   /// TODO: In-progress
   /// </summary>
   public static class BleAdvertisingUtils
   {
      internal const Byte AdvertisingPacketPreamble = 0xAA;
      internal const UInt32 AdvertisingPacketAccessAddress = 0x8E89BED6;

      /// <summary>
      /// Convert the raw struct from the <see cref="BlePacket" /> into a typed advertisement class
      /// </summary>
      internal static IBlePeripheral AsUndirectedAdvertisement( this AdvertisingChannelPdu pdu, Int32 rssi = 0 )
      {
         try
         {
            Log.Trace($"into BleAdvertisingUtils.AsUndirectedAdvertisement");
            if (pdu.Type.CanCarryPayload() && pdu.payload.Length >= 6)
            {
               Log.Trace($"    into pdu.Type.CanCarryPayload");
               var deviceGuid = new Byte[16];
               var address = pdu.payload.Slice(0, 6);
               address.CopyTo(deviceGuid, 10);
               return new BlePeripheral(
                  type: pdu.Type,
                  guid: new Guid(deviceGuid),
                  address: address,
                  addressIsRandom: pdu.TxAdd,
                  advertising: ParseAdvertisingPayloadData(
                     pdu.payload.Length > 6 ? DoSlice(pdu.payload, 7)
                     : new Byte[] { }),
                  rssi: rssi);
            }
         }
         catch (Exception e)
         {
            Log.Trace($"Exception in AsUndirectedAdvertisement: {e.Message}.");
         }

         return null;
      }

      internal static byte[] DoSlice(byte[] payload, int sliceSize)
      {
         var result = new Byte[] { };
         try
         {
            result = payload.Slice(sliceSize);
         }
         catch (Exception e)
         {
            Log.Trace($"Exception in DoSlice: {e.Message}.");
         }

         return result;

      }





      /// <summary>
      /// Syntax sugar for
      /// <c>packet.accessAddress == AdvertisingPacketAccessAddress &amp;&amp; packet.preamble == AdvertisingPacketPreamble</c>
      /// </summary>
      internal static Boolean IsAdvertisingChannelPDU( this BlePacket packet )
      {
         return packet.accessAddress == AdvertisingPacketAccessAddress && packet.preamble == AdvertisingPacketPreamble;
      }

      /// <summary>
      /// Parse <paramref name="advD" /> payload data from advertising packet.
      /// <remarks>You should never need to call this from client code, the BLE.net platform libraries handle it for you.</remarks>
      /// </summary>
      /// <exception cref="InvalidDataException">If the advertisement payload reports an incorrect length</exception>
      public static IList<AdvertisingDataItem> ParseAdvertisingPayloadData( Byte[] advD )
      {
         var records = new List<AdvertisingDataItem>();
         var index = 0;
         while(index < advD?.Length)
         {
            var length = advD[index];
            index++;
            if(length > 0)
            {
               if(!(advD.Length >= index + length))
               {
                  Log.Warn(
                     "BLE advertising payload incorrectly formatted: length {0} at index {1} would overflow the payload ({2} bytes total)",
                     length,
                     index,
                     advD.Length );
                  return records;
               }

               var type = advD[index];
               try
               {
                  var data = advD.Slice(index + 1, index + length);
                  index += length;
                  records.Add(new AdvertisingDataItem((AdvertisingDataType)type, data));
               }
               catch (Exception e)
               {
                     Log.Trace($"Exception in ParseAdvertisingPayloadData: {e.Message}.");
               }
            }
         }

         return records;
      }

      internal class BlePeripheral : AbstractBlePeripheral
      {
         /// <summary>
         /// A peripheral with an empty advertisement
         /// </summary>
         public static readonly BlePeripheral Empty = new BlePeripheral(
            type: AdvertisingType.ADV_IND,
            guid: Guid.Empty,
            address: new Byte[0],
            addressIsRandom: null,
            advertising: new List<AdvertisingDataItem>(),
            rssi: -1 );

         internal BlePeripheral( AdvertisingType type, Guid guid, Byte[] address, Boolean? addressIsRandom,
                                 IEnumerable<AdvertisingDataItem> advertising, Int32 rssi )
            : base( guid, address, addressIsRandom )
         {
            Log.Trace($"BlePeripheral created");
            Type = type;
            //Advertisement = new BleAdvertisement();
            Rssi = rssi;
         }

         /// <inheritdoc />
         public override IBleAdvertisement Advertisement { get; }

         /// <summary>
         /// The specific advertisement type. Must be a type that returns true for
         /// <see cref="AdvertisingTypeExtensions.CanCarryPayload" />
         /// </summary>
         public AdvertisingType Type { get; }
      }
   }
}
