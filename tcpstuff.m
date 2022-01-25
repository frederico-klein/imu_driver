% ---------------------------------------------
% This script allows for live acquistion and analyzes of one Myon EMG
% sensor data. To use this script the program "Playground" in Visual Studio
% must be run. That programs configures the data acquistion and writes data
% to the TCP server. 
% ---------------------------------------------
% Written by Katrin Emma Ammendrup
% Last updated 16-05-2018
% ---------------------------------------------
t = tcpclient('127.0.0.1', 5000); % Connect to TCP server

fs = 2000; % Myon Sample time 2000hz 

% Set for how long the live processing should last (in seconds)
endTime = 30; % Set the time of acquistion here
frameCount = 1;
RawEMG = zeros(1,fs*endTime); 
EMGwindow = 0;
n = 480; % Buffer size
p = 20; % Buffer overlap
k = 0; % Used to save the RawEMG data
figure
while(frameCount/fs <= endTime)
   RawEMG(frameCount) = read(t, 1, 'single'); % Read one sample at time
   EMGwindow = EMGwindow+1; % Collect samples in buffer of size n
   if EMGwindow == n
        y = RawEMG(k*n+1:(k+1)*n);  
        y_abs = abs(y); % Rectify
        y_mm = movmean(y_abs,120); % Moving average with window size 120
        y_FILTERED = y_mm(1:n-p);
        x = k*(n-p):k*(n-p)+n-p-1;
        time = x/fs;
        plot(time, y_FILTERED,'k')
        hold on
        axis([0, endTime,0,200])
        xlabel('Time [s]') % Labels make drawing slower
        ylabel('Amplitude [mV]')
        drawnow limitrate
        EMGwindow = 0; % Reset buffer
        k = k+1;
   end  
    frameCount = frameCount+1;
end