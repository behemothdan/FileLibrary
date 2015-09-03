// The following code is for reloading the modified image based off buttons clicked by appending query string and various other functions
var querystring = "";
var tempSource = "";
var lastModifiedImage = "";
var currentToolSelectionButton = "";
var currentToolSelectionValueButton = "";
var userBackgroundColor = "";
var searchRegex;
var fileNameSource = "";
var $imageToCrop = $('#modalImageEdit'), cropBoxData, setCanvasData;

var $searchGrid = $('#libraryContents').isotope({
	itemSelector: '.library-entry',
	layoutMode: 'fitRows',
	filter: function () {
		return searchRegex ? $(this).text().match(searchRegex) : true;
	}
});

// Text filtering
var $quicksearch = $('.quicksearch').keyup(debounce(function () {
	searchRegex = new RegExp($quicksearch.val(), "gi");
	$searchGrid.isotope();
}, 200));

$('#finish-upload').hide();
// dropzoneForm is the camel-case version of the upload form ID
// Show the "finished" button after all images have been uploaded.
// This will "refresh" the view, showing the new items in the library list and the success message
Dropzone.options.dropzoneForm = {
	init: function () {
		this.on("queuecomplete", function (file) {
			$('#finish-upload').show();
		});
	}
};

// Using this row for visual modification of the breadcrumb menu
$(".row .breadcrumb li:last-child").addClass("active");

// Activate Bootstrap's tooltips
$('[data-toggle="tooltip"]').tooltip();

// Loading images in modal windows
$('#modalContainer').on('show.bs.modal', function (event) {
	var buttonSource = $(event.relatedTarget)
	var imageSource = buttonSource.data('src')
	var imageSourceResized = buttonSource.data('src') + "?max=538"
	var fileNameSource = buttonSource.data('filename')
	var dateAddedSource = buttonSource.data('filedate')
	$('#modalImage').attr('src', imageSourceResized);
	$('#modalImageLink').attr('href', imageSource);
	$('#footerFileName').text(fileNameSource + " - Uploaded: " + dateAddedSource);
});

// Modal window for file deletion confirmation
$('#deleteConfirmation').on('show.bs.modal', function (event) {
	var buttonSource = $(event.relatedTarget)

	if (buttonSource.data('isfolder')) {
		$('.DeleteIsFolder').show();
		$('.DeleteIsFile').hide();
	}
	else {
		$('.DeleteIsFolder').hide();
		$('.DeleteIsFile').show();
	}
	$('#fileToDelete').text(buttonSource.data('name'));
	$('#DeletePath').attr("value", buttonSource.data('path'));
});

// Auto-dismiss alerts after 2 seconds
$(".alert-success").fadeTo(2000, 500).slideUp(500, function () {
	$(".alert-success").alert('close');
});

// Show and hide the search tools
$("#showSearchTools").click(function () {
	$(".search-tools").slideToggle("fast", function () {
		$("#showSearchTools").toggleClass("btn-success");
	});
});

// Show and hide the upload form
$("#showUploadTools").click(function () {
	$(".upload-tools").slideToggle("fast", function () {
		$("#showUploadTools").toggleClass("btn-success");
	});
});

// Loading images in image editor modal
$('#imageEditor').on('show.bs.modal', function (event) {
	var buttonSource = $(event.relatedTarget)
	var imageSource = buttonSource.data('src')
	fileNameSource = buttonSource.data('filename')
	var dateAddedSource = buttonSource.data('filedate')
	$("#originalImageName").val(fileNameSource);
	$('#modalImageEdit').attr('src', imageSource);
});

// Enable and disable the cropping tool
$('.crop').on("click", function () {
	$('.crop').toggleClass('btn-success');
	if ($('.crop').hasClass('btn-success')) {
		// If we toggle the btn-success class on, initalize the cropping tool and set an image size as well as define the crop container
		$imageToCrop.cropper({
			autoCropArea: 0.5,			
			minContainerHeight: 50,
			minContainerWidth: 50,
			built: function () {
				$imageToCrop.cropper('setCropBoxData', cropBoxData);
				$imageToCrop.cropper('setCanvasData', setCanvasData);
			}
		});
		disableEditingButtons();
	} else {
		// If they click the crop button again after being initalized, destroy the crop function, allowing the modal to be closed and another image to be opened
		cancelCrop();
	}
})

// Aspect ratio options
// We can create additional ratio options by simple creating new buttons and adding a data-ratio type with the fraction desired
$('.cropratio').on("click", function () {
	var cropRatio = $(this).attr("data-ratio");	
	$imageToCrop.cropper("setAspectRatio", eval(cropRatio));
})

// Kill the cropping tool in case they close the modal using the close button without deactivating it
$('#imageEditor').on('hide.bs.modal', function (event) {
	cancelCrop();
});

$('.cancel-crop').on("click", function () {
	cancelCrop();
});

// Function to disable the crop and perform other required tasks
function cancelCrop() {
	$('.crop').removeClass('btn-success');
	$imageToCrop.cropper('destroy', 'clear', 'reset');
	enableEditingButtons();
}

// The cropping tool doesn't play nice if we try to transform the image while cropping is enabled (which makes sense)
// Have the user finish cropping before being able to apply other transformations
function enableEditingButtons() {
	$('.rotateleft').prop("disabled", false);
	$('.rotateright').prop("disabled", false);
	$('.flip').prop("disabled", false);
	$('.resize').prop("disabled", false);
	$('.roundedcorners').prop("disabled", false);
	$('.saturation').prop("disabled", false);
	$('.crop-confirm-buttons').hide();
}

function disableEditingButtons() {
	$('.rotateleft').prop("disabled", true);
	$('.rotateright').prop("disabled", true);
	$('.flip').prop("disabled", true);
	$('.resize').prop("disabled", true);
	$('.roundedcorners').prop("disabled", true);
	$('.saturation').prop("disabled", true);
	$('.crop-confirm-buttons').show();
}

// For when we want the image URL without the query strings
function imagePathNoQueryString(url) {
	return url.split("?")[0];
}

// Check to see if the new image being edited is the same as the previous one
// If not we reset the query string variable so no edits carry over
function imageEditComparison(currentImageUrl) {
	// This resets the query string variable that will be temporarily stored if the user loads a new image
	if (imagePathNoQueryString(currentImageUrl) != lastModifiedImage) {
		// Also reset the sliders in addition to removing the query string			
		$('#resize').val("");
		$('#roundedCorners').val(0);
		$('#saturation').val(0)
		querystring = "";
	}

	// Sets the last image that we loaded for comparison
	lastModifiedImage = imagePathNoQueryString($('#modalImageEdit').attr("src"));
}

// When opening the saturation, resize, or rounded corners dropdown reset the sliders if it's a new image
$('.edit-container .dropdown-toggle').click(function () {
	tempSource = $('#modalImageEdit').attr("src");
	imageEditComparison(tempSource);
});

$('.resetBgColor').click(function () {
	$('#backgroundColorPicker').val("#000000");
	userBackgroundColor = "";
	if ($('#roundedCorners').val() != 0) {
		processImageEffect("roundedcorners", $('#roundedCorners').val());
	}
});

$('#backgroundColorPicker').on('change', function () {
	userBackgroundColor = $(this).val();
	if ($('#roundedCorners').val() != 0) {
		processImageEffect("roundedcorners", $('#roundedCorners').val(), userBackgroundColor);
	}
});

$('.edit-container .btn-group .btn').click(function () {
	currentToolSelectionButton = $(this).attr("data-editfunction");

	// Call the processImageEffect function if they didn't click on the saturation or rounded corners since we need to modify the input for those two
	if (currentToolSelectionButton != "saturation" && currentToolSelectionButton != "roundedcorners" && currentToolSelectionButton != "crop" && currentToolSelectionButton != "resize") {
		currentToolSelectionValueButton = $(this).attr("data-editvalues");
		processImageEffect(currentToolSelectionButton, currentToolSelectionValueButton);
	}
});

$('#roundedCorners').change(function () {
	if (this.value > 0) {
		// If the user has chosen a color, pass it to the processImageEffect function as well
		if (userBackgroundColor != "") {
			processImageEffect("roundedcorners", this.value, userBackgroundColor);
		} else {
			processImageEffect("roundedcorners", this.value);
		}
	} else if (this.value == 0) {
		// This passes in the clearThisQueryFlag to completely remove the roundedcorner if it's value is 0
		processImageEffect("roundedcorners", this.value, "", true)
	}
});

$('#saturation').change(function () {
	processImageEffect("saturation", this.value);
});

// The following two functions do the same thing allowing he user to use the step tool or type in a value
$("#resize").change(function () {
	processImageEffect("width", this.value);
});

// Use the debounce function again to prevent it updating every millisecond and give the user a chance to finish typing
$("#resize").keyup(debounce(function () {
	// If the user leaves the box empty clear the query flag so we don't have an empty width query string
	if ($('#resize').val() == "") {
		processImageEffect("width", $('#resize').val(), "", true);
	} else {
		processImageEffect("width", $('#resize').val());
	}
}, 200));

// We allow 4 different variables to be passed to the function currently
// The clearThisQueryFlag was added after finding some of the image processing tools didn't like 0 values (like the roundedcorner value) if the user tried to reset them
// We can pass in the clearThisQueryFlag (and the bgcolor as a color or empty string) as true if we want to remove the selected tools query string entirely
function processImageEffect(currentToolSelection, currentToolSelectionValue, optionalBackgroundColor, clearThisQueryFlag) {
	// Image Processor wants the color without the #
	if (optionalBackgroundColor != null && optionalBackgroundColor != "") {
		optionalBackgroundColor = optionalBackgroundColor.replace("#", "");
	}

	// Grab the currently loaded image
	tempSource = $('#modalImageEdit').attr("src");

	// Run check to see if the previous and current image are the same ones being edited
	imageEditComparison(tempSource);

	// If it's the first transformation button they clicked just apply
	if (querystring == "") {
		querystring = "?" + currentToolSelection + "=" + currentToolSelectionValue;
		if (currentToolSelection == "roundedcorners" && optionalBackgroundColor != null) {
			querystring += "&bgcolor=" + optionalBackgroundColor;
		}
	}
	else if (querystring != "") // If there is already a query string it means at least one transformation was already applied
	{
		// Pass a query string list into the URI tool
		var currentImageUrl = URI("?" + querystring);

		// Check to see if the transformation button that was clicked has already been applied
		if (currentImageUrl.hasQuery(currentToolSelection) === true) {
			// Remove that query string so we can reapply it in case the value changed
			// We don't want to remove it if we are rotating since we want to do math based off the image rotation button presses
			if (currentToolSelection != "rotate") {
				currentImageUrl.removeSearch(currentToolSelection);
			}

			// Add the chosen transformation along with the value from the button click
			// This portion skips adding the flip transform back because pressing it again flips the image back to it's original orientation
			// Also the clearThisQueryFlag being true will skip this section causing that string to not be readded essentially deleting it from the image URL
			if (currentToolSelection != "flip" && clearThisQueryFlag != true) {
				if (currentToolSelection == "rotate") {
					// Rotation math
					var currentParameters = URI.parseQuery("?" + querystring);
					var imageRotation = +currentParameters["rotate"] + +currentToolSelectionValue;
					if (imageRotation >= 360 || imageRotation <= -360) // We don't need numbers greater than 360 degrees or -360 degrees
					{
						imageRotation = 0;
					}
					currentImageUrl.setSearch(currentToolSelection, imageRotation);
				}
				else {
					// Assign any other transformations back to the query string
					currentImageUrl.setSearch(currentToolSelection, currentToolSelectionValue);
				}
			}

			// Reconstruct a usable query string
			querystring = currentImageUrl.build();
		}
		else // If a transformation button is clicked for the first time we can add it safely
		{
			querystring += "&" + currentToolSelection + "=" + currentToolSelectionValue;
		}

		// Rounded corner background color handling - possibly move to separate function
		// See if we have a roundedcorners query string and check if we are using that tool currently so we can check for updated bgcolors
		if (currentImageUrl.hasQuery("roundedcorners") === true && currentToolSelection == "roundedcorners") {
			if (currentImageUrl.hasQuery("bgcolor") === true) // If we currently have a background color query string remove it so we can recheck
			{
				currentImageUrl.removeSearch("bgcolor");
			}
			if (optionalBackgroundColor != null) // If they still have a background color selected reapply it otherwise leave the bgcolor query string off
			{
				querystring += "&bgcolor=" + optionalBackgroundColor;
			}
		}
	}
	// Since we are grabbing the image URL again and still storing the query string, clear it from the image source so we don't append twice
	tempSource = imagePathNoQueryString(tempSource);

	tempSource = tempSource + querystring;
	$('#modalImageEdit').attr("src", tempSource);
}

$('.dropdown-menu input').click(function (e) {
	e.stopPropagation(); //This will prevent the event from bubbling up and close the dropdown when you type/click on text boxes
});

// Debounce so filtering doesn't happen every millisecond
function debounce(fn, threshold) {
	var timeout;
	return function debounced() {
		if (timeout) {
			clearTimeout(timeout);
		}
		function delayed() {
			fn();
			timeout = null;
		}
		timeout = setTimeout(delayed, threshold || 50);
	}
};