# hadesFirm_Reborn
Based on https://github.com/ivanmeler/hadesFirm_Reborn

Usage:

Windows GUI program
  Start without arguments

Console mode program:
  Start with command line arguments
Usage:

Update check:
     hadesFirm.exe -c -model [device model] -region [region code] -imei [Imei or Serial number]
                [-version [pda/csc/phone/data]] [-binary]

Decrypting:
     hadesFirm.exe -file [path-to-file.zip.enc2] -version [pda/csc/phone/data] [-meta metafile]
     hadesFirm.exe -file [path-to-file.zip.enc4] -version [pda/csc/phone/data] -logicValue [logicValue] [-meta metafile]

Downloading:
     hadesFirm.exe -model [device model] -region [region code] -imei [Imei or Serial number]
                [-version [pda/csc/phone/data]] [-folder [output folder]]
                [-binary] [-autodecrypt] [-nozip] [-meta metafile]
                