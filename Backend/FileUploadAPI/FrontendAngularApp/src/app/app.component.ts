import { Component } from '@angular/core';
import { FileUploadComponent } from "./file-upload/file-upload.component";


@Component({
  selector: 'app-root',
  imports: [FileUploadComponent, FileUploadComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'file-upload-angular';
}
