﻿namespace BricksAppFunction.Utilities
{
    public static class PlainTextContent
    {
        public const string ManagePage = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
  <title>Bricks App</title>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <link rel=""stylesheet"" href=""https://maxcdn.bootstrapcdn.com/bootstrap/3.4.1/css/bootstrap.min.css"">
  <script src=""https://ajax.googleapis.com/ajax/libs/jquery/3.5.1/jquery.min.js""></script>
  <script src=""https://maxcdn.bootstrapcdn.com/bootstrap/3.4.1/js/bootstrap.min.js""></script>
  <script type=""text/javascript"">
    function onSubscriptionSubmit() {
        let urlRegex = new RegExp(""https://promoklocki\\.pl/lego-.*\\d{5}-.*p\\d*"");
        // let postUrl = ""http://localhost:7071/api/AddSubscription"";
        let postUrl = ""https://BricksApp.azurewebsites.net/api/AddSubscription"";

        let mail = document.getElementById(""subscriptionInputEmail"").value;
        let promoklockiUrlErrorMessage = document.getElementById(""promoklockiUrlErrorMessage"");
        let addSubscriptionSuccessMessage = document.getElementById(""addSubscriptionSuccessMessage"");
        let addSubscriptionFailMessage = document.getElementById(""addSubscriptionFailMessage"");
        let urlElement = document.getElementById(""subscriptionUrl"");
        let url = urlElement.value;
        let onlyBigUpdates = document.getElementById(""onlyBigUpdatesCheckbox"").checked;

        addSubscriptionSuccessMessage.style.display = ""none"";
        addSubscriptionFailMessage.style.display = ""none"";

        console.log(urlRegex.test(url));
        if(!urlRegex.test(url)) {
            promoklockiUrlErrorMessage.style.display = ""block"";
            addSubscriptionSuccessMessage.style.display = ""none"";
            addSubscriptionFailMessage.style.display = ""none"";
            return;
        }

        var data = JSON.stringify({""mail"": mail, ""url"": url, ""onlyBigUpdates"": onlyBigUpdates});

        var saveData = $.ajax({
            type: ""POST"",
            url: postUrl,
            data: data.toString(),
            dataType: ""text"",
            success: function(resultData){
                console.log(""success"");
                addSubscriptionSuccessMessage.style.display = ""block"";
                addSubscriptionFailMessage.style.display = ""none"";
                document.getElementById(""onlyBigUpdatesCheckbox"").checked = false;
                document.getElementById(""subscriptionUrl"").value = """";
            },
            error: function(XMLHttpRequest, textStatus, errorThrown){
                console.log(""error"");
                console.log(textStatus);
                console.log(errorThrown);
                addSubscriptionSuccessMessage.style.display = ""none"";
                addSubscriptionFailMessage.style.display = ""block"";
            }
        });

        promoklockiUrlErrorMessage.style.display = ""none"";
    }

    function onSubscriptionDelete() {
        //let postUrl = ""http://localhost:7071/api/DeleteSubscription"";
        let postUrl = ""https://BricksApp.azurewebsites.net/api/DeleteSubscription"";

        let mail = document.getElementById(""deleteSubscriptionInputEmail"").value;
        let number = document.getElementById(""deleteSubscriptionNumber"").value;

        var data = JSON.stringify({""mail"": mail, ""number"": number});

        var saveData = $.ajax({
            type: ""POST"",
            url: postUrl,
            data: data.toString(),
            dataType: ""text\plain"",
            success: function(resultData){
                console.log(""success"");
            }
        });

        document.getElementById(""deleteSubscriptionNumber"").value = """";
        document.getElementById(""deleteSubscriptionInputEmail"").value ="""";
    }

    window.onload = () => {
        let submitButton = document.getElementById(""submitButton"");
        submitButton.onclick = onSubscriptionSubmit;

        let deleteButton = document.getElementById(""deleteButton"");
        deleteButton.onclick = onSubscriptionDelete;
    }

    </script>
</head>
<body>

<div class=""container"">
    <div class=""form-group"">
        <h1>Add subscription</h1>
        <label for=""subscriptionInputEmail"">Your email address</label>
        <input type=""email"" class=""form-control"" id=""subscriptionInputEmail"" aria-describedby=""emailHelp"" placeholder=""Enter email"" />
        <br/>
        <label for=""subscriptionUrl"">Url of set you want to subscribe</label>
        <input type=""text"" class=""form-control"" id=""subscriptionUrl"" aria-describedby=""emailHelp"" placeholder=""Enter url from promoklocki"" />
        <p id=""promoklockiUrlErrorMessage"" style=""color:red; display:none;"">Link to Promoklocki is not valid. Correct example: https://promoklocki.pl/lego-creator-expert-10277-lokomotywa-crocodile-p20638</p>
        <br/>
        <div class=""form-check"">
            <input type=""checkbox"" class=""form-check-input"" id=""onlyBigUpdatesCheckbox"">
            <label class=""form-check-label"" for=""onlyBigUpdatesCheckbox"">Only big updates</label>
        </div>
        <br/>
        <button type=""submitAddSubscription"" class=""btn btn-primary"" id=""submitButton"">Submit</button>
        <p id=""addSubscriptionSuccessMessage"" style=""color:green; display:none;"">Subscription added successfully</p>
        <p id=""addSubscriptionFailMessage"" style=""color:red; display:none;"">Something went wrong</p>
    </div>
    <hr/>
    <div class=""form-group"">
        <h1>Delete subscription</h1>
        <label for=""deleteSubscriptionInputEmail"">Your email address</label>
        <input type=""email"" class=""form-control"" id=""deleteSubscriptionInputEmail"" aria-describedby=""emailHelp"" placeholder=""Enter email"" />
        <br/>
        <label for=""subscriptionUrl"">Catalog number</label>
        <input type=""text"" class=""form-control"" id=""deleteSubscriptionNumber"" aria-describedby=""emailHelp"" placeholder=""Catalog number"" />
        <br/>
        <button type=""submitDeleteSubscription"" class=""btn btn-danger"" id=""deleteButton"">Delete</button>
    </div>
    <hr/>
</div>

</body>
</html>
";
    }
}
