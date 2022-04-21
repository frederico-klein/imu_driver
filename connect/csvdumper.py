#!/usr/bin/env python

# -*- coding: utf-8 -*-
"""
Spyder Editor

This is a temporary script file.
"""
from tabulate import tabulate
import sys, traceback
import socket
import time
import csv

now = str(time.time()) + " "

class Sender:
    def __init__(self, FILENAME, hostname="0.0.0.0", period = 0.01, repeat = False):
        self.serverAddressPort   = (hostname, 8080 )
        self.bufferSize          = 4096
        self.period = period # in seconds
        # Create a UDP socket at client side
        self.UDPClientSocket = socket.socket(family=socket.AF_INET, type=socket.SOCK_DGRAM)
        self.FILENAME= FILENAME
        self.repeat = repeat
        self.range = 3 # number of imus to print
        self.rangelist = slice(18*self.range+1)

    def genmsg(self):
        NUM_IMU = 10
        #msgFromClient       = "Hello UDP Server"
        msgFromClient       = now + ("".join([(str(float(x/100))+" ")*NUM_IMU for x in range(0,18)])) 
        return msgFromClient

    def getreadfromcsv(self):
        with open(self.FILENAME) as csvfile:
            while(True):
                line = csv.reader(csvfile)
                next(line) ## trying to skip the header
                next(line) ## trying to skip the header
                next(line) ## trying to skip the header
                next(line) ## trying to skip the header
                if self.range is 1:
                    self.labels = [a.split("thorax_")[-1] for a in next(line)[self.rangelist]] ## this line has the actual labels, if you want them
                else:
                    self.labels = [a for a in next(line)[self.rangelist]] ## this line has the actual labels, if you want them

                for a in line:
                    if self.repeat:
                        ## need to use actual time, or it will break when i loop
                        a[0]=str(time.time())
                    # like use as a generator?
                    yield ["{:+.5f}".format(float(i)) for i in a][self.rangelist]
                if self.repeat:
                    csvfile.seek(0)
                else:
                    break

    #bytesToSend         = str.encode(msgFromClient)
    def loopsend(self):
        #try:
            self.UDPClientSocket.settimeout(0.1)
            
            for i,msg in enumerate(self.getreadfromcsv()):
                #if i > 200:
                #    break
                #print("\t".join(self.labels))
                print(tabulate([msg], headers=self.labels, floatfmt="+2.3f"))
                jointmsg = " ".join(msg)
                #print(jointmsg)
                #print(msg)
                bytesToSend = str.encode(jointmsg)
                # Send to server using created UDP socket
                RECVOK = False
                while not RECVOK:
                    try:

                        self.UDPClientSocket.sendto(bytesToSend, self.serverAddressPort)
                        msgFromServer = self.UDPClientSocket.recvfrom(self.bufferSize)
                        RECVOK = True
                    except socket.timeout:
                        print("stuck!")
                        time.sleep(self.period/2)
                        pass
                        
                msg_rec = "Message from Server {}".format(msgFromServer[0])
                #print(msg_rec)
                time.sleep(self.period)
            self.UDPClientSocket.sendto(str.encode("BYE!"), self.serverAddressPort)
            print("finished!")
        #except:
        #    traceback.print_exc(file=sys.stdout)



if __name__ == "__main__":

    A = Sender("gait1992_imu.csv", hostname="0.0.0.0", period=0.06)
    A.loopsend()
