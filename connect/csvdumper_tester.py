#!/usr/bin/env python
import csvdumper

# Send to server using created UDP socket
if __name__ == "__main__":

    B = csvdumper.Sender("mobl2016_imu.csv", hostname="0.0.0.0", period=0.01, repeat=True, msggen="genmsg")
    #B = csvdumper.Sender("mobl2016_imu.csv", hostname="0.0.0.0", period=0.01, repeat=True)#, msggen="genmsg")
    B.loopsend()
