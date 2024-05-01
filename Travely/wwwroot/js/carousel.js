$('.next-owl').click(function () {
    $('.owl-carousel').trigger('next.owl.carousel');
});

$('.prev-owl').click(function () {
    $('.owl-carousel').trigger('prev.owl.carousel');
});

document.body.addEventListener('htmx:afterSwap', function (event) {
    $('.owl-carousel').owlCarousel({
        margin: 10,
        nav: false,
        responsive: {
            0: {
                items: 1
            },
            425: {
                items: 2
            },
            860: {
                items: 3
            },
            1024: {
                items: 4
            }
        }
    })
});

document.body.addEventListener('htmx:afterOnLoad', function (event) {
    if (event.detail.xhr.status >= 200 && event.detail.xhr.status < 300 && (event.detail.pathInfo.requestPath === "/upload" || event.detail.pathInfo.requestPath.includes("/delete"))) {
        if (event.detail.pathInfo.requestPath === "/upload") {
            var myModal = bootstrap.Modal.getInstance(document.getElementById('UploadForm'));
            myModal.hide();
        }
        htmx.ajax('GET', '/categories', {target: '.owl-carousel', swap:'outerHTML'});
    }
});