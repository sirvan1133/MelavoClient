package org.melavo.vpn;
import android.content.*;
import android.database.*;
import android.net.Uri;
import android.os.ParcelFileDescriptor;
import java.io.*;
public final class UpdateProvider extends ContentProvider {
 public boolean onCreate(){return true;}
 public String getType(Uri uri){return "application/vnd.android.package-archive";}
 public Cursor query(Uri uri,String[] projection,String selection,String[] args,String sort){File file=new File(getContext().getFilesDir(),"updates/ready.apk");MatrixCursor c=new MatrixCursor(new String[]{"_display_name","_size"});c.addRow(new Object[]{"Melavo-update.apk",file.length()});return c;}
 public ParcelFileDescriptor openFile(Uri uri,String mode)throws FileNotFoundException{if(!"/apk".equals(uri.getPath())||!"r".equals(mode))throw new FileNotFoundException();return ParcelFileDescriptor.open(new File(getContext().getFilesDir(),"updates/ready.apk"),ParcelFileDescriptor.MODE_READ_ONLY);}
 public Uri insert(Uri uri,ContentValues values){throw new UnsupportedOperationException();}public int delete(Uri uri,String selection,String[] args){throw new UnsupportedOperationException();}public int update(Uri uri,ContentValues values,String selection,String[] args){throw new UnsupportedOperationException();}
}
