$('.next-owl').click(function () {
    $('.owl-carousel').trigger('next.owl.carousel');
});

$('.prev-owl').click(function () {
    $('.owl-carousel').trigger('prev.owl.carousel');
});
console.log('asdasda')

document.body.addEventListener('htmx:afterSwap', function (event) {
    console.log('alooooo')
    $('.owl-carousel').owlCarousel({
        margin: 10,
        items: 4,
        nav: false
    })
});

document.body.addEventListener('htmx:afterOnLoad', function (event) {
    if (event.detail.xhr.status >= 200 && event.detail.xhr.status < 300 && event.detail.pathInfo.requestPath === "/upload") {
        var myModal = bootstrap.Modal.getInstance(document.getElementById('AdminForm'));
        myModal.hide();
    }
});