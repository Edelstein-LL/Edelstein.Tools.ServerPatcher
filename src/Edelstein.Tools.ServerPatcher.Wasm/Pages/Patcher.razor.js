window.getRemoteFileStream = async function (url) {
    const response = await fetch(url);

    return response.body;
};

window.downloadFileFromDotnetStream = async function (filename, contentType, dotnetStreamRef) {
    const arrayBuffer = await dotnetStreamRef.arrayBuffer();

    const file = new File([arrayBuffer], filename, {type: contentType});
    const exportUrl = URL.createObjectURL(file);

    return exportUrl;
};

window.encryptAes = async function (inputArray, keyArray, ivArray) {
    const key = await window.crypto.subtle.importKey(
        "raw",
        keyArray,
        {
            name: "AES-CBC",
            length: 256
        },
        true,
        ["encrypt"]
    );

    const encryptedData = await window.crypto.subtle.encrypt(
        {
            name: "AES-CBC",
            iv: ivArray
        },
        key,
        inputArray
    );

    return new Uint8Array(encryptedData);
};

window.decryptAes = async function (encryptedArray, keyArray, ivArray) {
    const key = await window.crypto.subtle.importKey(
        "raw",
        keyArray,
        {
            name: "AES-CBC",
            length: 256
        },
        true,
        ["decrypt"]
    );

    // Decrypt the data
    const decryptedData = await window.crypto.subtle.decrypt(
        {
            name: "AES-CBC",
            iv: ivArray
        },
        key,
        encryptedArray
    );

    return new Uint8Array(decryptedData);
};
