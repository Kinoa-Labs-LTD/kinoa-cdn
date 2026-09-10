mergeInto(LibraryManager.library, {
    //flush our file changes to IndexedDB
    SyncFiles: function () {
        FS.syncfs(false, function (err) {
           if (err) console.log("syncfs error: " + err);
        });
    }
});