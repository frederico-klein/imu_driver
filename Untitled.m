figure
for i = 1:10000
    fs = 2000;
    if mod(i,480)==0
        y = out.simout.Data(i-479:i);
        y_abs = abs(y); % Rectify
        y_r=reshape(y_abs, [480 1]);
        y_mm = movmean(y_abs,120); % Moving average with window size 120
        y_FILTERED = y_mm(1:480);
        x = i- 1:i -1 +480-1;
        time = x/fs;
        plot(time, y_FILTERED,'k')
        hold on
        xlabel('Time [s]') % Labels make drawing slower
        ylabel('Amplitude [mV]')
        drawnow limitrate
        EMGwindow = 0; % Reset buffer
   end  
end

    