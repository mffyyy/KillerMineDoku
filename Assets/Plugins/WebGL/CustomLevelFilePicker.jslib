mergeInto(LibraryManager.library, {
  KillerMineDoku_RequestJsonImport: function (targetPtr, successMethodPtr, failureMethodPtr) {
    var target = UTF8ToString(targetPtr);
    var successMethod = UTF8ToString(successMethodPtr);
    var failureMethod = UTF8ToString(failureMethodPtr);

    var input = document.createElement("input");
    input.type = "file";
    input.accept = ".json,application/json";
    input.style.position = "fixed";
    input.style.left = "-9999px";
    input.style.top = "-9999px";
    input.setAttribute("aria-hidden", "true");

    var cleaned = false;
    var cleanup = function () {
      if (cleaned) {
        return;
      }
      cleaned = true;
      if (input.parentNode) {
        input.parentNode.removeChild(input);
      }
      window.removeEventListener("focus", focusHandler);
    };

    var focusHandler = function () {
      window.setTimeout(function () {
        if (!cleaned && (!input.files || input.files.length === 0)) {
          cleanup();
          SendMessage(target, failureMethod, "");
        }
      }, 500);
    };

    input.onchange = function () {
      if (!input.files || input.files.length === 0) {
        cleanup();
        SendMessage(target, failureMethod, "");
        return;
      }

      var file = input.files[0];
      var reader = new FileReader();
      reader.onload = function () {
        cleanup();
        SendMessage(target, successMethod, reader.result || "");
      };
      reader.onerror = function () {
        cleanup();
        SendMessage(target, failureMethod, "读取文件失败");
      };
      reader.readAsText(file, "utf-8");
    };

    document.body.appendChild(input);
    window.addEventListener("focus", focusHandler);
    input.click();
  }
});
