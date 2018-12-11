# Upload And Paste

This is a small Windows tool that allows you to paste the contents of the clipboard as plaintext (removing formatting).  Additionally it automatically uploads images and files on the clipboard to a server and pastes the url.  This provides functionality similar to CloudApp, except on your own server for free.  It supports FTP, SFTP, SCP, AWS S3, WebDav, via the included WinSCP library.

Here is an example screenshot from my dev machine uploaded this way: ![http://rog.gy/ss/dd4299790b.png](http://rog.gy/ss/dd4299790b.png)
I simply pressed Alt-PrintScreen on my keyboard to take a screenshot of my active app, then Ctrl-Shift-V to paste the URL of the uploaded screenshot.

## Functionality

  1.	If content of clipboard is a file, it uploads that file to the server and pastes the public URL.
  2.	If content of the clipboard is an image (such as a screenshot or other raw bitmap data), it saves to a png and uploads to the server, pasting the public URL.
  3.	If content of clipboard is rich text or HTML, it pastes the plain text without formatting.
  4.	If none of the above, it silently aborts.

In all supported cases it pastes a plain text, easily sharable representation of the content.

## Setup

1.	Either build from source in Visual Studio 2017 or download the pre-built binary from [http://rog.gy/share/UploadAndPaste.zip](http://rog.gy/share/UploadAndPaste.zip) (yes that was uploaded using this tool!)
2.	Rename one of the `server-config.example.json` files to `server-config.json` and fill in the details.
	* `"baseUploadPath": "/var/www/mysite/"`  
		This is the path relative to the root of your server where files should be uploaded

	* `"baseUrl":"https://example.com/"`  
		This is the root public URL that the files are accessible from.

	* `"fileDir": "share/"` *Optional*  
		You can specify a subdirectory where non-screenshot files are uploaded.  Since these files may be of any type, you should configure your server/host to serve these files directly without running/executing them (for example a shared hosting provider may assume you want to execute a php script, which may result in unforeseen issues)

	* `"ssDir": "ss/"` *Optional*  
		You can specify a subdirectory where screenshots uploaded.

	* The remaining items are configuration for WinSCP to connect to the server.  You can generate the values directly via WinSCP as documented here: https://winscp.net/eng/docs/ui_generateurl#code

3.	If you want to use a hotkey other than Ctrl-Shift-V, you need to modify `UploadAndPaste.hotkeyLoader.ahk` and use [AutoHotKey](https://www.autohotkey.com/) to recompile the script
4.	Run `UploadAndPaste.hotkeyLoader.exe` or set it to run on startup.  Depending on how you set it to run on startup, you may need to ensure the working directory is specified.
5.	With some data on the clipboard and a focused text area to paste into, press Ctrl-Shift-V to test it out.

## Known Issues

1.  When there are multiple files on the clipboard, it only uploads one.
2.  There is no progress indicatior, so large uploads may appear to stall on slow connections.  The mouse cursor changes to the wait cursor so you know it's working.

## Why

I created this tool to scratch my own itch.  I hope you find it useful!  Feel free to report any bugs or suggestions you may find.  More information about this [clipboard upload tool](https://rogerpincombe.com/upload-and-paste) on [my homepage](https://rogerpincombe.com).