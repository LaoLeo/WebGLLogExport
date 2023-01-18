

mergeInto(LibraryManager.library, {
    //flush our file changes to IndexedDB
    SyncDB: function () {
        FS.syncfs(false, function (err) {
            if (err) console.log("syncfs error: " + err);
        });
    },
    // 导出log文件
    ExportLogFile: function (path) {
		// 转string必须放在这个作用域，在下面的回调作用域里转会错误
		var loggerkey = UTF8ToString(path);
        FS.syncfs(false, function (err) {
            if (err) console.log("syncfs error: " + err);

            if (loggerkey == undefined || !loggerkey) return;
            const logFileName = loggerkey.substring(loggerkey.lastIndexOf("/") + 1);
			console.log("logger：", loggerkey, logFileName);
            const DBName = "/idbfs";
			const storeName = "FILE_DATA";
            const request = indexedDB.open(DBName);
            request.onsuccess = function (event) {
                const db = event.target.result;
                const tx = db.transaction(storeName, 'readonly');
                const store = tx.objectStore(storeName);
                // 获取数据
                const reqGet = store.get(loggerkey);
                reqGet.onsuccess = function (event) {
                    const contents = reqGet.result.contents;
                    const logContent = _ArrayBufferUTF8ToStr(contents.buffer);
                    console.log("log content:", logContent);
                    // 上报或导出文件
                    _SaveFile(logContent, "text/plain", logFileName);
                }
            }
        });
    },
    ArrayBufferUTF8ToStr: function (array) {
        var out, i, len, c;
        var char2, char3;
        if (array instanceof ArrayBuffer) {
            array = new Uint8Array(array);
        }

        out = "";
        len = array.length;
        i = 0;
        while (i < len) {
            c = array[i++];
            switch (c >> 4) {
                case 0: case 1: case 2: case 3: case 4: case 5: case 6: case 7:
                    // 0xxxxxxx
                    out += String.fromCharCode(c);
                    break;
                case 12: case 13:
                    // 110x xxxx   10xx xxxx
                    char2 = array[i++];
                    out += String.fromCharCode(((c & 0x1F) << 6) | (char2 & 0x3F));
                    break;
                case 14:
                    // 1110 xxxx  10xx xxxx  10xx xxxx
                    char2 = array[i++];
                    char3 = array[i++];
                    out += String.fromCharCode(((c & 0x0F) << 12) |
                        ((char2 & 0x3F) << 6) |
                        ((char3 & 0x3F) << 0));
                    break;
            }
        }

        return out;
    },
    // 保存文件
    SaveFile: function (value, type, name) {
        var blob;
        if (typeof window.Blob == "function") {
            blob = new Blob([value], {
                type: type
            });
        } else {
            var BlobBuilder = window.BlobBuilder || window.MozBlobBuilder || window.WebKitBlobBuilder || window.MSBlobBuilder;
            var bb = new BlobBuilder();
            bb.append(value);
            blob = bb.getBlob(type);
        }
        var URL = window.URL || window.webkitURL;
        var bloburl = URL.createObjectURL(blob);
        var anchor = document.createElement("a");
        if ('download' in anchor) {
            anchor.style.visibility = "hidden";
            anchor.href = bloburl;
            anchor.download = name;
            document.body.appendChild(anchor);
            var evt = document.createEvent("MouseEvents");
            evt.initEvent("click", true, true);
            anchor.dispatchEvent(evt);
            document.body.removeChild(anchor);
        } else if (navigator.msSaveBlob) {
            navigator.msSaveBlob(blob, name);
        } else {
            location.href = bloburl;
        }
    }
});