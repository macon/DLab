  var hostName = "com.macon.dlab.tabit";
  var port = chrome.runtime.connectNative(hostName);
  port.onMessage.addListener(onNativeMessage);
  port.onDisconnect.addListener(onDisconnected);
  console.log("Hello from background");
  console.log(port);

function onNativeMessage(message) {
  if (message.data.op === "get-tabinfo"){
    getTabInfo(sendTabInfoToNativePort)
  }
  if (message.data.op === "set-tab"){
    setTab(message.data.id)
  }
}

function onDisconnected() {
	console.log("Goodbye from background");
	port = null;
}

function sendTabInfoToNativePort(tabInfo){
	var msg = {"tabs": tabInfo};
  	sendNativeMessage(msg);
}

function sendNativeMessage(message){
  port.postMessage(message);
}

function setTab(id){
  chrome.tabs.update(parseInt(id), {selected: true});
}

function getTabInfo(callBack){
  var tabInfo = new Array();
  var queryInfo = {
      active: true,
  };

  chrome.tabs.query({}, 
    function(tabs) {
      for (var i = 0; i < tabs.length; i++) {
        var tab = tabs[i];
        tabInfo.push({"id": tab.id, "url": tab.url, "title": tab.title});
      };
      callBack(tabInfo);
  });
}
