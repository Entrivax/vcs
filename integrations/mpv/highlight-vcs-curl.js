mp.add_key_binding("h", highlight)

var serverUrl = 'http://localhost:33321/'

function highlight() {
    var time = mp.get_property_number("time-pos")
    var inFile = mp.get_property("stream-open-filename")
    // mp.utils.subprocess({ args: [
    //     'curl',
    //     '-X', 'POST',
    //     '-d', JSON.stringify({ FileName: inFile, Timestamp: time }),
    //     serverUrl
    // ], cancellable: false})
    var result = mp.command_native({
        name: 'subprocess',
        playback_only: false,
        capture_stdout: true,
        args: [
            'curl',
            '-s',
            '-o', '/dev/null',
            '-w', '%{http_code}',
            '-X', 'POST',
            '-d', JSON.stringify({ FileName: inFile, Timestamp: time }),
            serverUrl
        ]
    }).stdout
    if (result != '200') {
        var msg = "Failed to add highlight at " + secondsToTimestamp(time)
        mp.osd_message(msg, 2)
        mp.msg.info(msg)
    } else {
        var msg = "Added highlight at " + secondsToTimestamp(time)
        mp.osd_message(msg, 2)
        mp.msg.info(msg)
    }
}

function secondsToTimestamp(seconds, noMillis, alwaysHours, alwaysMinutes) {
    var hours = seconds >= 3600 || alwaysHours ? padStart(trunc((noMillis ? Math.round(seconds) : seconds) / 3600).toString(), 2, '0') + ':' : '';
    var minutes = seconds >= 60 || alwaysHours || alwaysMinutes ? padStart(trunc((noMillis ? Math.round(seconds) : seconds) % 3600 / 60).toString(), 2, '0') + ':' : '';
    var secondsSplit = ((noMillis ? Math.round(seconds) : seconds) % 60)
        .toFixed(noMillis ? 0 : 3)
        .split('.');
        mp.msg.info(secondsSplit[0])
    var secondsString = padStart(secondsSplit[0], 2, '0') + (secondsSplit.length > 1 ? '.' + secondsSplit[1] : '');
    return hours + minutes + secondsString
}

function padStart(str, targetLength,padString) {
    targetLength = targetLength>>0; //truncate if number or convert non-number to 0;
    padString = String((typeof padString !== 'undefined' ? padString : ' '));
    if (str.length > targetLength) {
        return String(str);
    }
    else {
        targetLength = targetLength-str.length;
        if (targetLength > padString.length) {
            padString += padString.repeat(targetLength/padString.length); //append to original to ensure we are longer than needed
        }
        return padString.slice(0,targetLength) + String(str);
    }
}

function trunc(v) {
    return v < 0 ? Math.ceil(v) : Math.floor(v);
};